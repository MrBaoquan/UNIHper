using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UNIHper.UI
{
    using UniRx;

    public class FileDialog : UIBase
    {
        enum DialogType
        {
            LoadFile,
            SaveFile
        }

        DialogType dialogType = DialogType.LoadFile;

        // Start is called before the first frame update
        private void Start()
        {
            EnableDragMove();
            this.Get<UPanel>()
                .OnCloseAsObservable()
                .Subscribe(_ =>
                {
                    Hide();
                });

            this.Get<Button>("button_group/btn_cancel")
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Hide();
                });

            var _fileName = this.Get<UInput>("input_fileName");
            var _openFileButton = this.Get<Button>("button_group/btn_open");
            _openFileButton
                .OnClickAsObservable()
                .Where(_ => _fileName.GetValue() != string.Empty)
                .Subscribe(_ =>
                {
                    if (OnOpenFileHandler != null)
                        OnOpenFileHandler(_fileName.GetValue());
                });

            var _saveFileButton = this.Get<Button>("button_group/btn_save");
            _saveFileButton
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    if (OnSaveFileHandler != null)
                        OnSaveFileHandler(_fileName.GetValue());
                });

            _fileName
                .OnValueChangedAsObservable()
                .Subscribe(_value =>
                {
                    if (dialogType == DialogType.LoadFile)
                    {
                        bool _containFile = fileInfos.Keys.Contains(_value);
                        if (!_containFile)
                        {
                            handleFilterFiles(_value);
                            if (lastSelected != null)
                                lastSelected.SetSelected(false);
                        }
                        else
                        {
                            handleFilterFiles(string.Empty);
                            Observable
                                .NextFrame()
                                .Subscribe(_ =>
                                {
                                    this.Get<ScrollRect>("scroll_fileList")
                                        .ScrollToCenter(
                                            fileItems[_value].GetComponent<RectTransform>(),
                                            RectTransform.Axis.Vertical
                                        );
                                });
                        }
                        _openFileButton.interactable = _containFile;
                    }
                });
        }

        private void handleFilterFiles(string InFilter)
        {
            InFilter = InFilter.ToLower();
            var _invalid = fileItems.Keys
                .Where(_key => !_key.ToLower().Contains(InFilter))
                .ToList();
            _invalid.ForEach(_key =>
            {
                fileItems[_key].gameObject.SetActive(false);
            });

            fileItems.Keys
                .Except(_invalid)
                .Where(_key => !fileItems[_key].gameObject.activeInHierarchy)
                .ToList()
                .ForEach(_key =>
                {
                    fileItems[_key].gameObject.SetActive(true);
                });
        }

        string TargetFileName = string.Empty;

        private void setTargetFileName(string InFileNmae)
        {
            this.Get<UInput>("input_fileName").SetValue(InFileNmae);
        }

        Action<string> OnOpenFileHandler = null;

        public IObservable<string> ReadFile(string InDir, string SearchPattern = "*.*")
        {
            dialogType = DialogType.LoadFile;
            syncLayout();
            LoadFilesInDirectory(InDir, SearchPattern);
            return Observable.FromEvent<string>(
                _action => OnOpenFileHandler += _action,
                _action => OnOpenFileHandler -= _action
            );
        }

        Action<string> OnSaveFileHandler = null;

        public IObservable<string> SaveFile(string InDir, string SearchPattern = "*.*")
        {
            dialogType = DialogType.SaveFile;
            syncLayout();
            LoadFilesInDirectory(InDir, SearchPattern);
            return Observable.FromEvent<string>(
                _action => OnSaveFileHandler += _action,
                _action => OnSaveFileHandler -= _action
            );
        }

        private void syncLayout()
        {
            if (dialogType == DialogType.LoadFile)
            {
                this.Get<UPanel>().SetTitle("打开文件");
                this.Get("button_group/btn_open").SetActive(true);
                this.Get("button_group/btn_save").SetActive(false);
            }
            else
            {
                this.Get<UPanel>().SetTitle("保存文件");
                this.Get("button_group/btn_save").SetActive(true);
                this.Get("button_group/btn_open").SetActive(false);
            }
        }

        public void LoadFilesInDirectory(string InDirectory, string SearchPattern = "*.*")
        {
            if (!Directory.Exists(InDirectory))
                return;
            syncFileList(new DirectoryInfo(InDirectory), SearchPattern);
        }

        public Dictionary<string, FileInfo> fileInfos = new Dictionary<string, FileInfo>();
        public Dictionary<string, FileItem> fileItems = new Dictionary<string, FileItem>();
        FileItem lastSelected = null;

        private void syncFileList(DirectoryInfo directoryInfo, string searchPattern = "*.*")
        {
            fileInfos.Clear();
            fileItems.Clear();

            var _content = this.Get("scroll_fileList/Viewport/Content");
            _content.Children().ForEach(_file => Destroy(_file.gameObject));

            var _searchPatterns = searchPattern
                .Split('|')
                .Select(_pattern => _pattern.Replace("*", ""));

            fileInfos = directoryInfo
                .GetFiles("*.*", SearchOption.TopDirectoryOnly)
                .Where(_fileInfo => _searchPatterns.Contains(_fileInfo.Extension.ToLower()))
                .ToDictionary(_fileInfo => _fileInfo.Name, _fileInfo => _fileInfo);

            var _fileItem = Managements.Resource.Get<GameObject>("FileItem");
            fileInfos.Keys
                .ToList()
                .ForEach(_fileName =>
                {
                    var _newFileItem = Instantiate(_fileItem, _content);
                    var _script = _newFileItem.GetComponent<FileItem>();
                    fileItems.Add(_fileName, _script);
                    _newFileItem
                        .GetComponent<Button>()
                        .OnClickAsObservable()
                        .Subscribe(_ =>
                        {
                            selectFile(_fileName);
                        });
                    _script.SetFileName(_fileName);
                });
        }

        private void selectFile(string InFileName)
        {
            setTargetFileName(InFileName);
            if (lastSelected != null)
                lastSelected.SetSelected(false);

            if (!fileItems.Keys.Contains(InFileName))
                return;

            var _script = fileItems[InFileName];
            _script.SetSelected(true);
            lastSelected = _script;
        }

        // Update is called once per frame
        private void Update() { }

        // Called when this ui is showing
        protected override void OnShown() { }

        // Called when this ui is hidden
        protected override void OnHidden() { }
    }
}
