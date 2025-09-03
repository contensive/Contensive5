console.log('block bootstrap javascript focusin trap to allow wysiwyg dialog tools');
document.addEventListener('DOMContentLoaded', function() {
  document.addEventListener('focusin', function (e) {
    e.stopImmediatePropagation(); // Prevent Bootstrap from hijacking focus
  }, true); 
});