using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace DNHper {

    public static class IOManger {

        public static IObservable < (string filename, int copied, int total) > CopyDir (string sourceDir, string destDir) {
            var _cts = new CancellationTokenSource ();

            return Observable.Create < (string, int, int) > (subscriber => {
                if (!Directory.Exists (destDir)) {
                    Directory.CreateDirectory (destDir);
                }
                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories (sourceDir, "*.*", SearchOption.AllDirectories)) {
                    Directory.CreateDirectory (dirPath.Replace (sourceDir, destDir));
                }

                int i = 0;
                var _files = Directory.GetFiles (sourceDir, "*.*", SearchOption.AllDirectories);
                var _total = _files.Length;
                //Copy all the files & Replaces any files with the same name
                try {
                    foreach (string newPath in _files) {
                        _cts.Token.ThrowIfCancellationRequested ();
                        subscriber.OnNext ((newPath, ++i, _total));
                        File.Copy (newPath, newPath.Replace (sourceDir, destDir), true);
                    }
                } catch (System.Exception exc) {
                    subscriber.OnError (exc);
                }
                subscriber.OnCompleted ();
                return new CancellationDisposable (_cts);
            });
        }

    }

}