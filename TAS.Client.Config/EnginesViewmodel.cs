﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Client.Common;

namespace TAS.Client.Config
{
    public class EnginesViewmodel: OkCancelViewmodelBase<Model.Engines>
    {
        readonly UICommand _commandAdd;
        readonly UICommand _commandDelete;
        public EnginesViewmodel(string connectionString, string connectionStringSecondary)
            : base(new Model.Engines(connectionString, connectionStringSecondary), new EnginesView(), "Engines")
        {
            _engines = new ObservableCollection<EngineViewmodel>(Model.EngineList.Select(e => new EngineViewmodel(e)));
            _engines.CollectionChanged += _engines_CollectionChanged;
            _commandAdd = new UICommand() { ExecuteDelegate = _add };
            _commandDelete = new UICommand() { ExecuteDelegate = o => _engines.Remove(_selectedEngine), CanExecuteDelegate = o => _selectedEngine != null };
        }

        private void _engines_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Model.EngineList.Add(((EngineViewmodel)e.NewItems[0]).Model);
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                Model.EngineList.Remove(((EngineViewmodel)e.OldItems[0]).Model);
                Model.DeletedEngines.Add(((EngineViewmodel)e.OldItems[0]).Model);
            }
            _isCollectionCanged = true;
        }

        private void _add(object obj)
        {
            var newEngine = new Model.Engine() { Servers = Model.Servers, ArchiveDirectories = Model.ArchiveDirectories };
            Model.EngineList.Add(newEngine);
            var newPlayoutServerViewmodel = new EngineViewmodel(newEngine);
            _engines.Add(newPlayoutServerViewmodel);
            SelectedEngine = newPlayoutServerViewmodel;            
        }

        public override void Save(object destObject = null)
        {
            foreach (EngineViewmodel e in _engines)
                e.Save();
            Model.Save();
            base.Save(destObject);
        }
        
        protected override void OnDispose()
        {
            _engines.CollectionChanged -= _engines_CollectionChanged;
        }

        private bool _isCollectionCanged;
        public override bool IsModified
        {
            get
            {
                return _isCollectionCanged 
                    || (_engines!= null && _engines.Any(e => e.IsModified));
            }
        }
        readonly ObservableCollection<EngineViewmodel> _engines;

        public ObservableCollection<EngineViewmodel> Engines { get { return _engines; } }
        EngineViewmodel _selectedEngine;
        public EngineViewmodel SelectedEngine { get { return _selectedEngine; } set { SetField(ref _selectedEngine, value, nameof(SelectedEngine)); } }
        public ICommand CommandAdd { get { return _commandAdd; } }
        public ICommand CommandDelete { get { return _commandDelete; } }

    }
}
