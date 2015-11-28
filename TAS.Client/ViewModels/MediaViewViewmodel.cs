﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;

namespace TAS.Client.ViewModels
{
    public class MediaViewViewmodel: ViewmodelBase
    {
        public readonly IMedia Media;
        public MediaViewViewmodel(IMedia media)
        {
            Media = media;
            Media.PropertyChanged += OnMediaPropertyChanged;
            if (Media is IPersistentMedia)
            {
                (Media as IPersistentMedia).MediaSegments.CollectionOperation += new EventHandler<CollectionOperationEventArgs<IMediaSegment>>(_mediaSegmentsCollectionOperation);
                foreach (IMediaSegment ms in (Media as IPersistentMedia).MediaSegments)
                    _mediaSegments.Add(new MediaSegmentViewmodel((Media as IPersistentMedia), ms));
            }
        }

        protected override void OnDispose()
        {
            Media.PropertyChanged -= OnMediaPropertyChanged;
            if (_mediaSegments != null && Media is IPersistentMedia)
                (Media as IPersistentMedia).MediaSegments.CollectionOperation -= new EventHandler<CollectionOperationEventArgs<IMediaSegment>>(_mediaSegmentsCollectionOperation);
        }

        public string MediaName { get { return Media.MediaName; } }
        public string FileName { get { return Media.FileName; } }
        public string Location { get { return Media.Directory.DirectoryName; } }
        public TimeSpan TCStart { get { return Media.TCStart; } }
        public TimeSpan TCPlay { get { return Media.TCPlay; } }
        public TimeSpan Duration { get { return Media.Duration; } }
        public TimeSpan DurationPlay { get { return Media.DurationPlay; } }
        public string sTCStart { get { return Media.TCStart.ToSMPTETimecodeString(Media.FrameRate); } }
        public string sTCPlay { get { return Media.TCPlay.ToSMPTETimecodeString(Media.FrameRate); } }
        public string sDuration { get { return Media.Duration.ToSMPTETimecodeString(Media.FrameRate); } }
        public string sDurationPlay { get { return Media.DurationPlay.ToSMPTETimecodeString(Media.FrameRate); } }
        public DateTime LastUpdated { get { return Media.LastUpdated; } }
        public TMediaCategory MediaCategory { get { return Media.MediaCategory; } }
        public TMediaStatus MediaStatus { get { return Media.MediaStatus; } }
        public TMediaEmphasis MediaEmphasis { get { return (Media is IPersistentMedia) ? (Media as IPersistentMedia).MediaEmphasis : TMediaEmphasis.None; } }
        public int SegmentCount { get { return (Media is IPersistentMedia) ? (Media as IPersistentMedia).MediaSegments.Count : 0; } }
        public bool HasSegments { get { return SegmentCount != 0; } }
        public bool IsTrimmed { get { return TCPlay != TCStart || Duration != DurationPlay; } }
        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    if (!value)
                        SelectedSegment = null;
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        private ObservableCollection<MediaSegmentViewmodel> _mediaSegments = new ObservableCollection<MediaSegmentViewmodel>();

        public ObservableCollection<MediaSegmentViewmodel> MediaSegments { get { return _mediaSegments; } }

        private void _mediaSegmentsCollectionOperation(object o, CollectionOperationEventArgs<IMediaSegment> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (e.Operation == TCollectionOperation.Insert)
                        _mediaSegments.Add(new MediaSegmentViewmodel((Media as IPersistentMedia), e.Item));
                    if (e.Operation == TCollectionOperation.Remove)
                    {
                        var segment = _mediaSegments.FirstOrDefault(ms => ms.MediaSegment == e.Item);
                        if (segment != null)
                            _mediaSegments.Remove(segment);
                    }
                    NotifyPropertyChanged("HasSegments");
                    if ((Media is IPersistentMedia) && (Media as IPersistentMedia).MediaSegments.Count == 0)
                        IsExpanded = false;
                }));
        }

        private MediaSegmentViewmodel _selectedSegment;
        public MediaSegmentViewmodel SelectedSegment
        {
            get { return _selectedSegment; }
            set
            {
                if (_selectedSegment != value)
                {
                    _selectedSegment = value;
                    NotifyPropertyChanged("SelectedSegment");
                }
            }
        }

        private void OnMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName) && GetType().GetProperty(e.PropertyName) != null)
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == "TCPlay"
                || e.PropertyName == "TCStart"
                || e.PropertyName == "Duration"
                || e.PropertyName == "DurationPlay")
                NotifyPropertyChanged("IsTrimmed");
            if (e.PropertyName == "FrameRate")
            {
                NotifyPropertyChanged("sTCPlay");
                NotifyPropertyChanged("sTCStart");
                NotifyPropertyChanged("sDuration");
                NotifyPropertyChanged("sDurationPlay");
            }
        }

        public override string ToString()
        {
            return Media.ToString();
        }
    }
}