(() => {
    const filter = document.getElementById('loc-mau');
    if (!filter) return;
    const rows = [...document.querySelectorAll('tr[data-mau]')];
    const boxes = [...document.querySelectorAll('input[name="serial"]')];
    filter.addEventListener('change', () => {
        rows.forEach(row => { row.hidden = !!filter.value && row.dataset.mau !== filter.value; });
    });
    const update = () => {
        const selected = boxes.filter(box => box.checked);
        document.getElementById('serial-da-chon').textContent = selected.length
            ? `Đã chọn ${selected.length} chiếc: ${selected.map(box => box.value).join(', ')}` : 'Chưa chọn chiếc nào.';
        const total = selected.reduce((sum, box) => sum + Number(box.dataset.gia), 0);
        document.getElementById('tong-tai-quay').textContent = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(total);
    };
    boxes.forEach(box => box.addEventListener('change', update));
    update();
})();
