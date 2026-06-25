// CONFIGURATION
let API_BASE_URL = 'http://localhost:5271'; // Default fallback

// GLOBAL STATE
let selectedDocumentId = null;
let documentsList = [];
let pollingInterval = null;
let currentPage = 1;
const pageSize = 4; // Sayfa başına gösterilecek belge sayısı

// DOM ELEMENTS
const uploadZone = document.getElementById('upload-zone');
const fileInput = document.getElementById('file-input');
const uploadProgress = document.getElementById('upload-progress');
const progressFill = document.getElementById('progress-fill');
const progressText = document.getElementById('progress-text');

const docListContainer = document.getElementById('doc-list');
const btnRefreshDocs = document.getElementById('btn-refresh-docs');

const currentDocTitle = document.getElementById('current-doc-title');
const currentDocStatus = document.getElementById('current-doc-status');
const chatMessages = document.getElementById('chat-messages');
const chatForm = document.getElementById('chat-form');
const chatInput = document.getElementById('chat-input');
const btnSend = document.getElementById('btn-send');

const inspectTotalChunks = document.getElementById('inspect-total-chunks');
const chunksList = document.getElementById('chunks-list');

// INITIALIZATION
document.addEventListener('DOMContentLoaded', async () => {
    initEvents();
    await detectApiBaseUrl();
    loadDocuments();
});

// Dynamic API Endpoint Detection (Handles HTTP vs HTTPS startup profiles in Visual Studio)
async function detectApiBaseUrl() {
    const endpoints = [
        'http://localhost:5271',
        'https://localhost:7247'
    ];
    
    for (const url of endpoints) {
        try {
            const controller = new AbortController();
            const id = setTimeout(() => controller.abort(), 800); // 800ms timeout
            
            const response = await fetch(`${url}/api/documents`, { 
                method: 'GET',
                signal: controller.signal 
            });
            clearTimeout(id);
            
            if (response.ok) {
                API_BASE_URL = url;
                console.log('Successfully connected to API at:', API_BASE_URL);
                
                // Update active indicator if it exists
                const indicator = document.querySelector('.api-status');
                if (indicator) {
                    indicator.innerHTML = '<span class="status-indicator online"></span><span>Sunucu Aktif</span>';
                }
                return;
            }
        } catch (e) {
            // Ignore error and try the next endpoint
        }
    }
    console.warn('API connection could not be established on known endpoints. Using fallback:', API_BASE_URL);
    const indicator = document.querySelector('.api-status');
    if (indicator) {
        indicator.innerHTML = '<span class="status-indicator" style="background-color: var(--color-error); box-shadow: 0 0 10px var(--color-error)"></span><span style="color: var(--color-error)">Sunucu Çevrimdışı</span>';
    }
}

function initEvents() {
    // Refresh Documents Button
    btnRefreshDocs.addEventListener('click', () => loadDocuments());

    // Drag and Drop Events on Upload Zone
    uploadZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        uploadZone.classList.add('dragover');
    });

    uploadZone.addEventListener('dragleave', () => {
        uploadZone.classList.remove('dragover');
    });

    uploadZone.addEventListener('drop', (e) => {
        e.preventDefault();
        uploadZone.classList.remove('dragover');
        if (e.dataTransfer.files.length > 0) {
            handleFileUpload(e.dataTransfer.files[0]);
        }
    });

    // Click to Upload Event
    uploadZone.addEventListener('click', (e) => {
        // Prevent click trigger if clicking progress bar
        if (!e.target.closest('#upload-progress')) {
            fileInput.click();
        }
    });

    fileInput.addEventListener('change', () => {
        if (fileInput.files.length > 0) {
            handleFileUpload(fileInput.files[0]);
            fileInput.value = ''; // Clear value for consecutive uploads of same file
        }
    });

    // Chat Form Submit
    chatForm.addEventListener('submit', handleChatSubmit);

    // Pagination Buttons
    const btnPrevPage = document.getElementById('btn-prev-page');
    const btnNextPage = document.getElementById('btn-next-page');
    if (btnPrevPage && btnNextPage) {
        btnPrevPage.addEventListener('click', () => {
            if (currentPage > 1) {
                currentPage--;
                renderDocumentList();
            }
        });
        btnNextPage.addEventListener('click', () => {
            const totalPages = Math.ceil(documentsList.length / pageSize) || 1;
            if (currentPage < totalPages) {
                currentPage++;
                renderDocumentList();
            }
        });
    }
}

// FORMATTING UTILITIES
function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleString('tr-TR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, function(m) { return map[m]; });
}

// 1. FILE UPLOAD HANDLER
function handleFileUpload(file) {
    // Validate file type
    const allowedExtensions = /(\.pdf|\.txt)$/i;
    if (!allowedExtensions.exec(file.name)) {
        alert('Yalnızca PDF veya TXT dosyaları yüklenebilir.');
        return;
    }

    // Validate size (15MB)
    if (file.size > 15 * 1024 * 1024) {
        alert('Dosya boyutu 15MB\'ı aşamaz.');
        return;
    }

    // Reset and show progress
    uploadProgress.style.display = 'block';
    progressFill.style.width = '0%';
    progressText.textContent = 'Hazırlanıyor...';

    const formData = new FormData();
    formData.append('file', file);

    const xhr = new XMLHttpRequest();
    xhr.open('POST', `${API_BASE_URL}/api/documents/upload`, true);

    // Progress handler
    xhr.upload.addEventListener('progress', (e) => {
        if (e.lengthComputable) {
            const percentComplete = Math.round((e.loaded / e.total) * 100);
            progressFill.style.width = percentComplete + '%';
            progressText.textContent = `Yükleniyor: %${percentComplete}`;
        }
    });

    // Load handler
    xhr.addEventListener('load', () => {
        uploadProgress.style.display = 'none';
        if (xhr.status >= 200 && xhr.status < 300) {
            try {
                const response = JSON.parse(xhr.responseText);
                console.log('Upload success:', response);
                
                // Refresh list and select the newly uploaded file
                loadDocuments(response.documentId);
            } catch (err) {
                console.error('Error parsing response:', err);
                alert('Dosya yüklendi fakat sunucu yanıtı işlenemedi.');
                loadDocuments();
            }
        } else {
            console.error('Upload failed with status:', xhr.status, xhr.statusText);
            alert(`Dosya yüklenemedi: ${xhr.responseText || xhr.statusText}`);
            loadDocuments();
        }
    });

    // Error handler
    xhr.addEventListener('error', () => {
        uploadProgress.style.display = 'none';
        alert('Yükleme sırasında ağ hatası oluştu.');
        loadDocuments();
    });

    xhr.send(formData);
}

// 2. DOCUMENT FETCH & RENDER
async function loadDocuments(selectNewId = null) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/documents`);
        if (!response.ok) {
            throw new Error(`HTTP Hata: ${response.status}`);
        }
        
        documentsList = await response.json();
        
        if (selectNewId) {
            currentPage = 1; // Reset to page 1 to show the newly uploaded document at the top
        }
        
        renderDocumentList(selectNewId);
        
        // Check if any document is in 'Processing' status
        const hasProcessingDocs = documentsList.some(d => d.status === 'Processing');
        if (hasProcessingDocs) {
            startPolling();
        } else {
            stopPolling();
        }
    } catch (err) {
        console.error('Dokümanlar yüklenirken hata oluştu:', err);
        docListContainer.innerHTML = '<div class="list-empty" style="color: var(--color-error)">Dokümanlar listelenirken bir hata oluştu. Sunucunun aktif olduğundan emin olun.</div>';
    }
}

function renderDocumentList(selectNewId = null) {
    const totalPages = Math.ceil(documentsList.length / pageSize) || 1;
    
    // Clamp current page
    if (currentPage > totalPages) {
        currentPage = totalPages;
    }
    if (currentPage < 1) {
        currentPage = 1;
    }

    const docPagination = document.getElementById('doc-pagination');
    const pageIndicator = document.getElementById('page-indicator');
    const btnPrevPage = document.getElementById('btn-prev-page');
    const btnNextPage = document.getElementById('btn-next-page');

    if (documentsList.length === 0) {
        docListContainer.innerHTML = '<div class="list-empty">Henüz yüklenmiş bir doküman bulunmuyor.</div>';
        if (docPagination) docPagination.style.display = 'none';
        
        // Reset current doc selection
        selectedDocumentId = null;
        updateChatHeader(null);
        renderInspector(null);
        return;
    }

    // Sort: newest first
    documentsList.sort((a, b) => new Date(b.uploadedAt) - new Date(a.uploadedAt));

    // Slice for pagination
    const startIndex = (currentPage - 1) * pageSize;
    const pageItems = documentsList.slice(startIndex, startIndex + pageSize);

    docListContainer.innerHTML = '';
    
    pageItems.forEach(doc => {
        const isSelected = selectedDocumentId === doc.id || (selectNewId === doc.id);
        if (selectNewId === doc.id) {
            selectedDocumentId = doc.id;
        }

        const card = document.createElement('div');
        card.className = `doc-card ${isSelected ? 'active' : ''}`;
        card.dataset.id = doc.id;

        // Status styling class
        let statusClass = 'status-processing';
        let statusText = 'İşleniyor';
        if (doc.status === 'Completed') {
            statusClass = 'status-completed';
            statusText = 'Hazır';
        } else if (doc.status === 'Failed') {
            statusClass = 'status-failed';
            statusText = 'Hata';
        }

        // File icon SVG (different for txt vs pdf)
        const isTxt = doc.contentType === 'text/plain';
        const fileIconSvg = isTxt 
            ? `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line></svg>`
            : `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line><polyline points="10 9 9 9 8 9"></polyline></svg>`;

        card.innerHTML = `
            <div class="doc-card-main">
                <div class="doc-card-icon">${fileIconSvg}</div>
                <div class="doc-card-details">
                    <div class="doc-card-title" title="${escapeHtml(doc.fileName)}">${escapeHtml(doc.fileName)}</div>
                    <div class="doc-card-meta">${formatDate(doc.uploadedAt)}</div>
                </div>
            </div>
            <div class="doc-card-actions">
                <span class="doc-status-badge ${statusClass}">${statusText}</span>
                <div style="display: flex; align-items: center; gap: 8px;">
                    <span class="doc-card-chunks">${doc.chunkCount} Parça</span>
                    <button class="btn-icon btn-delete" title="Belgeyi Sil" data-id="${doc.id}">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                            <polyline points="3 6 5 6 21 6"></polyline>
                            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                              <line x1="10" y1="11" x2="10" y2="17"></line>
                              <line x1="14" y1="11" x2="14" y2="17"></line>
                        </svg>
                    </button>
                </div>
            </div>
            ${doc.errorMessage ? `<div style="font-size: 10px; color: var(--color-error); padding-top: 4px; border-top: 1px dashed rgba(239,68,68,0.1); word-break: break-word;">${escapeHtml(doc.errorMessage)}</div>` : ''}
        `;

        // Card click handler
        card.addEventListener('click', (e) => {
            if (e.target.closest('.btn-delete')) return; // Ignore if delete is clicked
            
            // Remove active classes
            document.querySelectorAll('.doc-card').forEach(c => c.classList.remove('active'));
            card.classList.add('active');
            
            selectedDocumentId = doc.id;
            updateChatHeader(doc);
            renderInspector(doc);
        });

        // Delete button click handler
        const btnDelete = card.querySelector('.btn-delete');
        btnDelete.addEventListener('click', async (e) => {
            e.stopPropagation();
            if (confirm(`"${doc.fileName}" belgesini ve bu belgeye ait tüm veritabanı vektör parçalarını silmek istediğinize emin misiniz?`)) {
                await deleteDocument(doc.id);
            }
        });

        docListContainer.appendChild(card);
    });

    // Update Pagination UI
    if (docPagination && pageIndicator && btnPrevPage && btnNextPage) {
        if (totalPages > 1) {
            docPagination.style.display = 'flex';
            pageIndicator.textContent = `Sayfa ${currentPage} / ${totalPages}`;
            btnPrevPage.disabled = (currentPage === 1);
            btnNextPage.disabled = (currentPage === totalPages);
        } else {
            docPagination.style.display = 'none';
        }
    }

    // Maintain selection state or select first completed
    if (selectedDocumentId) {
        const currentDoc = documentsList.find(d => d.id === selectedDocumentId);
        if (currentDoc) {
            updateChatHeader(currentDoc);
            renderInspector(currentDoc);
        } else {
            // Document was probably deleted
            selectedDocumentId = null;
            updateChatHeader(null);
            renderInspector(null);
        }
    } else {
        // Default: If no selected doc, display "All Documents" or the latest Completed doc
        updateChatHeader(null);
        renderInspector(null);
    }
}

// 3. POLLING FOR ASYNC DOCUMENT PROCESSING
function startPolling() {
    if (pollingInterval) return;
    console.log('Polling started...');
    pollingInterval = setInterval(async () => {
        try {
            const response = await fetch(`${API_BASE_URL}/api/documents`);
            if (response.ok) {
                const newList = await response.json();
                
                // Compare status of documents to decide if we should redraw
                let hasChanges = false;
                if (newList.length !== documentsList.length) {
                    hasChanges = true;
                } else {
                    for (let i = 0; i < newList.length; i++) {
                        const oldDoc = documentsList.find(d => d.id === newList[i].id);
                        if (!oldDoc || oldDoc.status !== newList[i].status || oldDoc.chunkCount !== newList[i].chunkCount) {
                            hasChanges = true;
                            break;
                        }
                    }
                }

                if (hasChanges) {
                    documentsList = newList;
                    renderDocumentList();
                    
                    // If selected doc was updated, reload its chunks in inspector
                    if (selectedDocumentId) {
                        const activeDoc = documentsList.find(d => d.id === selectedDocumentId);
                        if (activeDoc) {
                            renderInspector(activeDoc);
                        }
                    }
                }

                // Stop polling if none are processing
                const stillProcessing = newList.some(d => d.status === 'Processing');
                if (!stillProcessing) {
                    stopPolling();
                }
            }
        } catch (err) {
            console.error('Polling error:', err);
        }
    }, 2000);
}

function stopPolling() {
    if (pollingInterval) {
        clearInterval(pollingInterval);
        pollingInterval = null;
        console.log('Polling stopped.');
    }
}

// 4. DOCUMENT DELETION HANDLER
async function deleteDocument(docId) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/documents/${docId}`, {
            method: 'DELETE'
        });
        
        if (response.ok) {
            if (selectedDocumentId === docId) {
                selectedDocumentId = null;
            }
            await loadDocuments();
        } else {
            const errText = await response.text();
            alert(`Silme başarısız: ${errText}`);
        }
    } catch (err) {
        console.error('Belge silinirken hata oluştu:', err);
        alert('Ağ hatası nedeniyle belge silinemedi.');
    }
}

// 5. UPDATE PANEL HEADERS
function updateChatHeader(doc) {
    if (doc) {
        currentDocTitle.textContent = doc.fileName;
        currentDocStatus.textContent = doc.status === 'Completed' 
            ? 'Seçili Belge Bağlamında RAG Modu' 
            : `Durum: ${doc.status === 'Processing' ? 'İşleniyor' : 'Hatalı'}`;
    } else {
        // Look for the latest completed document in the list to indicate what default fallback is
        const latestCompleted = documentsList.find(d => d.status === 'Completed');
        if (latestCompleted) {
            currentDocTitle.textContent = 'En Son Belge';
            currentDocStatus.textContent = `Küresel RAG: "${latestCompleted.fileName}" temel alınacaktır`;
        } else {
            currentDocTitle.textContent = 'Tüm Dokümanlar';
            currentDocStatus.textContent = 'Küresel RAG Modu (Önce bir doküman yükleyin)';
        }
    }
}

// 6. DB INSPECTOR CHUNKS LIST LOADER
async function renderInspector(doc) {
    if (!doc) {
        inspectTotalChunks.textContent = '0';
        chunksList.innerHTML = '<div class="list-empty">Lütfen sol taraftaki kütüphaneden ham parçalarını incelemek istediğiniz dokümanı seçin.</div>';
        return;
    }

    inspectTotalChunks.textContent = doc.chunkCount;

    if (doc.status === 'Processing') {
        chunksList.innerHTML = '<div class="list-empty">Doküman henüz işleniyor. Parçalar veritabanına kaydedildiğinde otomatik listelenecektir.</div>';
        return;
    }

    if (doc.status === 'Failed') {
        chunksList.innerHTML = '<div class="list-empty" style="color: var(--color-error)">Doküman vektörleştirilemediği için incelenecek ham parça bulunamadı.</div>';
        return;
    }

    chunksList.innerHTML = '<div class="list-empty">Parçalar veritabanından çekiliyor...</div>';

    try {
        const response = await fetch(`${API_BASE_URL}/api/documents/${doc.id}/chunks`);
        if (!response.ok) {
            throw new Error(`HTTP Hata: ${response.status}`);
        }
        
        const chunks = await response.json();
        
        if (chunks.length === 0) {
            chunksList.innerHTML = '<div class="list-empty">Bu belgenin veritabanında saklanan herhangi bir parçası bulunamadı.</div>';
            return;
        }

        chunksList.innerHTML = '';
        
        chunks.forEach((chunk, index) => {
            const chunkCard = document.createElement('div');
            chunkCard.className = 'chunk-card';
            
            // Format coordinates / chunk metadata nicely
            chunkCard.innerHTML = `
                <div class="chunk-meta">
                    <span class="chunk-index">Parça #${chunk.order + 1}</span>
                    <span class="chunk-vector-badge">${chunk.embeddingDimension}d Vektör</span>
                </div>
                <div class="chunk-content">${escapeHtml(chunk.content)}</div>
            `;
            chunksList.appendChild(chunkCard);
        });

    } catch (err) {
        console.error('Ham parçalar yüklenirken hata oluştu:', err);
        chunksList.innerHTML = '<div class="list-empty" style="color: var(--color-error)">Vektör hücreleri yüklenemedi.</div>';
    }
}

// 7. CHAT (RAG) HANDLER
async function handleChatSubmit(e) {
    e.preventDefault();
    
    const question = chatInput.value.trim();
    if (!question) return;

    // Append user message
    appendMessage('user', question);
    chatInput.value = '';
    
    // Disable inputs during request
    chatInput.disabled = true;
    btnSend.disabled = true;

    // Create typing indicator placeholder
    const typingIndicator = appendTypingIndicator();
    chatMessages.scrollTop = chatMessages.scrollHeight;

    try {
        const payload = {
            question: question,
            documentId: selectedDocumentId // Sends null if no specific document is selected
        };

        const response = await fetch(`${API_BASE_URL}/api/ai/ask`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        // Remove typing indicator
        typingIndicator.remove();

        if (!response.ok) {
            throw new Error(`HTTP Hata: ${response.status}`);
        }

        const data = await response.json();
        
        // Append bot message with answer and sources
        appendMessage('assistant', data.answer, data.sources);

    } catch (err) {
        console.error('AI yanıtı alınırken hata oluştu:', err);
        typingIndicator.remove();
        appendMessage('assistant', 'Üzgünüm, sorunuzu işlerken sunucu tarafında bir hata oluştu. Lütfen bağlantınızı ve Ollama servisinizin durumunu kontrol edin.');
    } finally {
        chatInput.disabled = false;
        btnSend.disabled = false;
        chatInput.focus();
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }
}

function appendMessage(sender, text, sources = []) {
    const msgDiv = document.createElement('div');
    msgDiv.className = `message ${sender}`;

    const contentDiv = document.createElement('div');
    contentDiv.className = 'message-content';

    // Format code blocks and linebreaks simple rendering
    let formattedText = escapeHtml(text).replace(/\n/g, '<br>');
    contentDiv.innerHTML = `<p>${formattedText}</p>`;

    // If sources are returned, render them
    if (sources && sources.length > 0) {
        const toggleBtn = document.createElement('button');
        toggleBtn.className = 'sources-toggle';
        toggleBtn.innerHTML = `
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="10" height="10">
                <polyline points="9 18 15 12 9 6"></polyline>
            </svg>
            Kaynakları Göster (${sources.length})
        `;

        const sourcesList = document.createElement('div');
        sourcesList.className = 'sources-list';
        sourcesList.style.display = 'none';

        sources.forEach((src) => {
            const srcItem = document.createElement('div');
            srcItem.className = 'source-item';
            
            // Format distance as percentage or distance metric
            const distanceText = src.cosineDistance !== undefined 
                ? `Mesafe: ${src.cosineDistance.toFixed(4)}` 
                : '';

            srcItem.innerHTML = `
                <div class="source-item-meta">
                    <span>Parça #${src.order + 1}</span>
                    <span class="source-item-score">${distanceText}</span>
                </div>
                <div>${escapeHtml(src.content)}</div>
            `;
            sourcesList.appendChild(srcItem);
        });

        // Wire up toggle button event
        toggleBtn.addEventListener('click', () => {
            const isVisible = sourcesList.style.display !== 'none';
            sourcesList.style.display = isVisible ? 'none' : 'flex';
            toggleBtn.classList.toggle('open', !isVisible);
            toggleBtn.innerHTML = `
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" width="10" height="10">
                    <polyline points="9 18 15 12 9 6"></polyline>
                </svg>
                ${isVisible ? 'Kaynakları Göster' : 'Kaynakları Gizle'} (${sources.length})
            `;
            // Scroll chat to accommodate expanded details
            setTimeout(() => {
                chatMessages.scrollTop = chatMessages.scrollHeight;
            }, 50);
        });

        contentDiv.appendChild(toggleBtn);
        contentDiv.appendChild(sourcesList);
    }

    msgDiv.appendChild(contentDiv);
    chatMessages.appendChild(msgDiv);
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

function appendTypingIndicator() {
    const msgDiv = document.createElement('div');
    msgDiv.className = 'message assistant typing-placeholder';

    const contentDiv = document.createElement('div');
    contentDiv.className = 'message-content';
    contentDiv.innerHTML = `
        <div class="typing-dots">
            <span></span>
            <span></span>
            <span></span>
        </div>
    `;

    msgDiv.appendChild(contentDiv);
    chatMessages.appendChild(msgDiv);
    return msgDiv;
}
