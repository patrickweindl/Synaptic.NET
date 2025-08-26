const sidebar = document.getElementById('stores');
const toggle = document.getElementById('toggleSidebar');
if(toggle && sidebar){
    toggle.addEventListener('click', () => {
        sidebar.classList.toggle('collapsed');
    });
}
