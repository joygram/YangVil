mergeInto(LibraryManager.library, {
  SyncFileSystem: function () {
    FS.syncfs(false, function (err) {
      if (err) console.warn("SyncFileSystem", err);
    });
  }
});
