mergeInto(LibraryManager.library, {
  KhCssWidth: function () {
    var stage = document.getElementById("stage");
    var w = stage && stage.clientWidth ? stage.clientWidth : (window.innerWidth || 0);
    return w | 0;
  },
  KhCssHeight: function () {
    var stage = document.getElementById("stage");
    var h = stage && stage.clientHeight ? stage.clientHeight : (window.innerHeight || 0);
    return h | 0;
  }
});
