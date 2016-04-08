﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class ExportViewmodel : ViewmodelBase
    {
        public ObservableCollection<ExportMediaViewmodel> Items { get; private set; }
        public ICommand CommandExport { get; private set; }
        readonly IMediaManager _mediaManager;
        Views.ExportView _view;
        public ExportViewmodel(IMediaManager mediaManager, IEnumerable<ExportMedia> exportList)
        {
            mediaManager.ReferenceAdd();
            _mediaManager = mediaManager;
            Items = new ObservableCollection<ExportMediaViewmodel>(exportList.Select(media => new ExportMediaViewmodel(mediaManager, media)));
            Directories = mediaManager.IngestDirectories.Where(d => d.IsExport).ToList();
            SelectedDirectory = Directories.FirstOrDefault();
            CommandExport = new UICommand() { ExecuteDelegate = _export, CanExecuteDelegate = _canExport };
            this._view = new Views.ExportView() { DataContext = this, Owner = System.Windows.Application.Current.MainWindow, ShowInTaskbar=false };
            _view.ShowDialog();
        }

        

        public List<IIngestDirectory> Directories { get; private set; }

        IIngestDirectory _selectedDirectory;
        public IIngestDirectory SelectedDirectory
        {
            get { return _selectedDirectory; }
            set
            {
                if (SetField(ref _selectedDirectory, value, "SelectedDirectory"))
                {
                    NotifyPropertyChanged("CommandExport");
                }
            }
        }

        void _export (object o)
        {
            _checking = true;
            NotifyPropertyChanged("CommandExport");
            try
            {
                //TODO: check if exporting files fit in device free space
            }
            finally
            {
                _checking = false;
                NotifyPropertyChanged("CommandExport");
            }
            _mediaManager.Export(Items.Select(mevm => mevm.MediaExport), false, string.Empty, SelectedDirectory);
            _view.Close();
        }

        bool _checking;
        bool _canExport(object o)
        {
            return !_checking && Items.Count > 0 && SelectedDirectory != null;
        }

        public int ExportMediaCount { get { return Items.Count; } }
        public TimeSpan TotalTime { get { return TimeSpan.FromTicks(Items.Sum(m => m.Duration.Ticks)); } }

        protected override void OnDispose()
        {
            _view = null;
            _mediaManager.ReferenceRemove();
        }
    }
}
