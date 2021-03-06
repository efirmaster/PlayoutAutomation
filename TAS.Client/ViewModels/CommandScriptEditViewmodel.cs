﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Views;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class CommandScriptEditViewmodel : EditViewmodelBase<ICommandScript>
    {
        private readonly IEvent _event;
        private readonly RationalNumber _frameRate;
        public CommandScriptEditViewmodel(IEvent aEvent, ICommandScript model) : base(model, new CommandScriptEditView())
        {
            _event = aEvent;
            _frameRate = _event.Engine.FrameRate;
            CommandAddCommandScriptItem = new UICommand { ExecuteDelegate = _addCommandScriptItem, CanExecuteDelegate = _canAddCommandScriptItem };
            CommandAddFinalizationCommandScriptItem = new UICommand { ExecuteDelegate = _addFinalizationCommandScriptItem, CanExecuteDelegate = _canAddCommandScriptItem };
            CommandDeleteCommandScriptItem = new UICommand { ExecuteDelegate = _deleteCommandScriptItem, CanExecuteDelegate = _canDeleteCommandScriptItem };
            CommandEditCommandScriptItem = new UICommand { ExecuteDelegate = _editCommandScriptItem, CanExecuteDelegate = _canEditCommandScriptItem };
            _commands = new ObservableCollection<CommandScriptItemViewmodel>(model.Commands.Select(csi => new CommandScriptItemViewmodel(csi, _frameRate)));
            foreach (var command in _commands)
                command.Modified += _command_Modified;
            _commands.CollectionChanged += _commands_CollectionChanged;
            _commandsView = CollectionViewSource.GetDefaultView(_commands);
            _commandsView.SortDescriptions.Add(new SortDescription(nameof(CommandScriptItemViewmodel.ExecuteTime), ListSortDirection.Ascending));
        }

        private void _command_Modified(object sender, EventArgs e)
        {
            IsModified = true;
        }

        private void _commands_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                foreach (CommandScriptItemViewmodel item in e.OldItems)
                    item.Modified -= _command_Modified;
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                foreach (CommandScriptItemViewmodel item in e.NewItems)
                    item.Modified += _command_Modified;
            IsModified = true;
            InvalidateRequerySuggested();
        }

        public override void Save(object destObject = null)
        {
            Model.Commands = Commands.Select(csivm => csivm.Model).ToList();
        }

        protected override void OnDispose()
        {
            foreach (CommandScriptItemViewmodel item in _commands)
                item.Modified -= _command_Modified;
        }

        private CommandScriptItemViewmodel _selectedCommand;
        public CommandScriptItemViewmodel SelectedCommand { get { return _selectedCommand; }
        set
            {
                if (_selectedCommand != value)
                {
                    _selectedCommand = value;
                    InvalidateRequerySuggested();
                }
            }
        }

        private readonly ObservableCollection<CommandScriptItemViewmodel> _commands;
        private readonly ICollectionView _commandsView;

        public ObservableCollection<CommandScriptItemViewmodel> Commands
        {
            get { return _commands; }
        }
        public ICommand CommandEditCommandScriptItem { get; private set; }
        public ICommand CommandAddCommandScriptItem { get; private set; }
        public ICommand CommandDeleteCommandScriptItem { get; private set; }
        public ICommand CommandAddFinalizationCommandScriptItem { get; private set; }

        private bool _canEditCommandScriptItem(object obj)
        {
            return SelectedCommand != null;
        }

        private void _editCommandScriptItem(object obj)
        {
            if (_selectedCommand.ShowDialog() == true && _selectedCommand.IsModified)
                IsModified = true;
        }

        private bool _canDeleteCommandScriptItem(object obj)
        {
            return SelectedCommand != null;
        }

        private void _deleteCommandScriptItem(object obj)
        {
            _commands.Remove(_selectedCommand);
        }

        private bool _canAddCommandScriptItem(object obj)
        {
            return true;
        }
        
        private void _addCommandScriptItem(object obj)
        {
            CommandScriptItemViewmodel newItem = new CommandScriptItemViewmodel(new CommandScriptItemBase(){ ExecuteTime = TimeSpan.Zero }, _frameRate);
            if (newItem.ShowDialog() == true)
            {
                if (newItem.ExecuteTime == null)
                    newItem.ExecuteTime = TimeSpan.Zero;
                _commands.Add(newItem);
            }
        }

        private void _addFinalizationCommandScriptItem(object obj)
        {
            CommandScriptItemViewmodel newItem = new CommandScriptItemViewmodel(new CommandScriptItemBase(), _frameRate);
            if (newItem.ShowDialog() == true)
                _commands.Add(newItem);
        }

    }
}
