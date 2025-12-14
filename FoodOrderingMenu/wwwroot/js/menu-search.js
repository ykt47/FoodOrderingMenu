$(function () {
    const $keyword = $("#keyword");
    const $category = $("#category");
    const $minPrice = $("#minPrice");
    const $maxPrice = $("#maxPrice");
    const $results = $("#results");
    let timer = null;
    const debounceMs = 250;

    function escapeHtml(s) { return $('<div/>').text(s || '').html(); }

    function render(items) {
        if (!items || items.length === 0) {
            $results.html('<div class="col-12"><p>No results found.</p></div>');
            return;
        }
        let html = '';
        items.forEach(it => {
            html += `
              <div class="col-md-4 mb-3">
                <div class="card h-100">
                  ${it.imageUrl ? `<img src="${it.imageUrl}" class="card-img-top" style="height:160px;object-fit:cover" />` : ''}
                  <div class="card-body">
                    <h5 class="card-title">${escapeHtml(it.name)}</h5>
                    <p class="card-text">${escapeHtml(it.description)}</p>
                    <p class="mb-1"><strong>Category:</strong> ${escapeHtml(it.category)}</p>
                    <p class="mb-1"><strong>Price:</strong> RM ${parseFloat(it.price).toFixed(2)}</p>
                    <p>${it.isAvailable ? '<span class="badge bg-success">Available</span>' : '<span class="badge bg-secondary">Not available</span>'}</p>
                  </div>
                </div>
              </div>`;
        });
        $results.html(html);
    }

    function showError(msg) {
        $results.html(`<div class="col-12"><p class="text-danger">${msg}</p></div>`);
    }

    function search() {
        clearTimeout(timer);
        timer = setTimeout(() => {
            $.getJSON('/MenuSearch/Search', {
                keyword: $keyword.val(),
                categoryId: $category.val(),
                minPrice: $minPrice.val(),
                maxPrice: $maxPrice.val()
            })
                .done(data => render(data))
                .fail((xhr, status, err) => {
                    let txt = "Error fetching results.";
                    if (xhr && xhr.responseJSON && xhr.responseJSON.error) txt = xhr.responseJSON.error;
                    showError(txt);
                    console.error("AJAX error", status, err, xhr);
                });
        }, debounceMs);
    }

    $keyword.on('input', search);
    $category.on('change', search);
    $minPrice.on('input', search);
    $maxPrice.on('input', search);

    // initial load
    search();
});
