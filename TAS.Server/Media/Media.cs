﻿#undef DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using TAS.Common;
using System.Diagnostics;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using Newtonsoft.Json;
using System.Threading;
using TAS.Remoting.Server;

namespace TAS.Server
{
    public abstract class Media : DtoBase, IMedia
    {

        public Media(IMediaDirectory directory, Guid mediaGuid = default(Guid))
        {
            _directory = (MediaDirectory)directory;
            _mediaGuid = mediaGuid == default(Guid)? Guid.NewGuid() : mediaGuid;
            _directory.MediaAdd(this);
        }

#if DEBUG
        ~Media()
        {
            Debug.WriteLine("{0} finalized: {1}", GetType(), this);
        }
#endif

        // file properties
        protected string _folder = string.Empty;
        [JsonProperty()]
        public string Folder
        {
            get { return _folder; }
            set
            {
                if (SetField(ref _folder, value, nameof(Folder)))
                    NotifyPropertyChanged(nameof(FullPath));
            }
        }
#region IMediaProperties
        protected string _fileName = string.Empty;
        [JsonProperty]
        public string FileName
        {
            get { return _fileName; }
            set
            {
                if (SetField(ref _fileName, value, nameof(FileName)))
                {
                    NotifyPropertyChanged(nameof(FullPath));
                }
            }
        }

        protected UInt64 _fileSize;
        [JsonProperty]
        public UInt64 FileSize 
        {
            get { return _fileSize; }
            set { SetField(ref _fileSize, value, nameof(FileSize)); }
        }

        protected DateTime _lastUpdated;
        [JsonProperty]
        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            set { SetField(ref _lastUpdated, value, nameof(LastUpdated)); }
        }
        //// to enable LastAccess: "FSUTIL behavior set disablelastaccess 0" on NTFS volume
        //// not stored in datebase
        //protected DateTime _lastAccess;
        //public DateTime LastAccess
        //{
        //    get { return _lastAccess; }
        //    internal set
        //    {
        //        if (_lastAccess != value)
        //        {
        //            _lastAccess = value;
        //            NotifyPropertyChanged("LastAccess");
        //        }
        //    }
        //}

        // media parameters
        protected string _mediaName;
        [JsonProperty]
        public virtual string MediaName
        {
            get { return _mediaName; }
            set { SetField(ref _mediaName, value, nameof(MediaName)); }
        }

        protected TMediaType _mediaType;
        [JsonProperty]
        public virtual TMediaType MediaType
        {
            get { return _mediaType; }
            set { SetField(ref _mediaType, value, nameof(MediaType)); }
        }

        protected TimeSpan _duration;
        [JsonProperty]
        public virtual TimeSpan Duration
        {
            get { return _duration; }
            set { SetField(ref _duration, value, nameof(Duration)); }
        }
        protected TimeSpan _durationPlay;
        [JsonProperty]
        public virtual TimeSpan DurationPlay
        {
            get { return _durationPlay; }
            set { SetField(ref _durationPlay, value, nameof(DurationPlay)); }
        }
        protected TimeSpan _tcStart;
        [JsonProperty]
        public virtual TimeSpan TcStart 
        {
            get { return _tcStart; }
            set { SetField(ref _tcStart, value, nameof(TcStart)); }
        }
        protected TimeSpan _tcPlay;
        [JsonProperty]
        public virtual TimeSpan TcPlay
        {
            get { return _tcPlay; }
            set { SetField(ref _tcPlay, value, nameof(TcPlay)); }
        }
        protected TVideoFormat _videoFormat;
        [JsonProperty]
        public virtual TVideoFormat VideoFormat
        {
            get { return _videoFormat; }
            set
            {
                if (SetField(ref _videoFormat, value, nameof(VideoFormat)))
                {
                    _videoFormatDescription = null;
                    NotifyPropertyChanged(nameof(FrameRate));
                    NotifyPropertyChanged(nameof(VideoFormatDescription));
                }
            }
        }
        protected bool _fieldOrderInverted;
        [JsonProperty]
        public bool FieldOrderInverted
        {
            get { return _fieldOrderInverted; }
            set { SetField(ref _fieldOrderInverted, value, nameof(FieldOrderInverted)); }
        }

        protected TAudioChannelMapping _audioChannelMapping;
        [JsonProperty]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public virtual TAudioChannelMapping AudioChannelMapping 
        {
            get { return _audioChannelMapping; }
            set { SetField(ref _audioChannelMapping, value, nameof(AudioChannelMapping)); }
        }
        protected decimal _audioVolume;
        [JsonProperty]
        public virtual decimal AudioVolume // correction amount on play
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value, nameof(AudioVolume)); }
        }

        protected decimal _audioLevelIntegrated;
        [JsonProperty]
        public virtual decimal AudioLevelIntegrated //measured
        {
            get { return _audioLevelIntegrated; }
            set { SetField(ref _audioLevelIntegrated, value, nameof(AudioLevelIntegrated)); }
        }

        protected decimal _audioLevelPeak;
        [JsonProperty]
        public virtual decimal AudioLevelPeak //measured
        {
            get { return _audioLevelPeak; }
            set { SetField(ref _audioLevelPeak, value, nameof(AudioLevelPeak)); }
        }

        protected TMediaCategory _mediaCategory;
        [JsonProperty]
        public virtual TMediaCategory MediaCategory
        {
            get { return _mediaCategory; }
            set { SetField(ref _mediaCategory, value, nameof(MediaCategory)); }
        }

        protected TParental _parental;
        [JsonProperty]
        public virtual TParental Parental
        {
            get { return _parental; }
            set { SetField(ref _parental, value, nameof(Parental)); }
        }

#endregion //IMediaProperties

        protected Guid _mediaGuid;
        [JsonProperty]
        public virtual Guid MediaGuid
        {
            get { return _mediaGuid; }
            internal set { SetField(ref _mediaGuid, value, nameof(MediaGuid)); }
        }


        protected VideoFormatDescription _videoFormatDescription;
        [JsonProperty]
        public VideoFormatDescription VideoFormatDescription
        {
            get
            {
                if (_videoFormatDescription == null) 
                    if (!VideoFormatDescription.Descriptions.TryGetValue(VideoFormat, out _videoFormatDescription))
                        _videoFormatDescription = VideoFormatDescription.Descriptions[TVideoFormat.Other];
                return _videoFormatDescription;
            }
            internal set { _videoFormatDescription = value; }
        }

        protected readonly MediaDirectory _directory;
        public IMediaDirectory Directory
        {
            get { return _directory; }
        }

        [JsonProperty]
        public string FullPath
        {
            get
            {
                return 
                    string.IsNullOrWhiteSpace(_folder) ?
                    string.Join(_directory.PathSeparator.ToString(), _directory.Folder.TrimEnd(_directory.PathSeparator), _fileName) :
                    string.Join(_directory.PathSeparator.ToString(), _directory.Folder.TrimEnd(_directory.PathSeparator), _folder, _fileName);
            }
            internal set
            {
                string relativeName = value.Substring(_directory.Folder.Length);
                FileName = Path.GetFileName(relativeName);
                Folder = relativeName.Substring(0, relativeName.Length - _fileName.Length).Trim(_directory.PathSeparator);
            }
        }

        public virtual bool Delete()
        {
            return ((MediaDirectory)Directory).DeleteMedia(this);
        }

        public virtual bool RenameTo(string newFileName)
        {
            try
            {
                if (_mediaStatus == TMediaStatus.Available 
                    && !newFileName.ToLowerInvariant().Equals(_fileName.ToLowerInvariant()))
                    File.Move(FullPath, Path.Combine(Path.GetDirectoryName(FullPath), newFileName));
                return true;
            }
            catch { }
            return false;
        }

        protected TMediaStatus _mediaStatus;
        [JsonProperty]
        public TMediaStatus MediaStatus
        {
            get { return _mediaStatus; }
            set { SetField(ref _mediaStatus, value, nameof(MediaStatus)); }
        }

        internal bool HasExtraLines; // VBI lines that shouldn't be displayed

        public virtual void CloneMediaProperties(IMedia fromMedia)
        {
            MediaName = fromMedia.MediaName;
            AudioChannelMapping = fromMedia.AudioChannelMapping;
            AudioVolume = fromMedia.AudioVolume;
            AudioLevelIntegrated = fromMedia.AudioLevelIntegrated;
            AudioLevelPeak = fromMedia.AudioLevelPeak;
            Duration = fromMedia.Duration;
            DurationPlay = fromMedia.DurationPlay;
            TcStart = fromMedia.TcStart;
            TcPlay = fromMedia.TcPlay;
            VideoFormat = fromMedia.VideoFormat;
            MediaCategory = fromMedia.MediaCategory;
            Parental = fromMedia.Parental;
        }

        public virtual Stream GetFileStream(bool forWrite)
        {
            return new FileStream(FullPath, forWrite ? FileMode.Create : FileMode.Open);
        }

        public virtual bool CopyMediaTo(Media destMedia, ref bool abortCopy)
        {
            bool copyResult = true;
            if ((!(_directory is IngestDirectory) || ((IngestDirectory)_directory).AccessType == TDirectoryAccessType.Direct)
                && (!(destMedia.Directory is IngestDirectory) || ((IngestDirectory)destMedia.Directory).AccessType == TDirectoryAccessType.Direct))
            {
                File.Copy(FullPath, destMedia.FullPath, true);
                File.SetCreationTimeUtc(destMedia.FullPath, File.GetCreationTimeUtc(FullPath));
                File.SetLastWriteTimeUtc(destMedia.FullPath, File.GetLastWriteTimeUtc(FullPath));
            }
            else
            {
                try
                {
                    if (_directory is IngestDirectory)
                        (_directory as IngestDirectory).LockXDCAM(true);
                    if (destMedia.Directory is IngestDirectory)
                        (destMedia.Directory as IngestDirectory).LockXDCAM(true);
                    using (Stream source = GetFileStream(false),
                                    dest = destMedia.GetFileStream(true))
                    {
                        var buffer = new byte[1024 * 1024];
                        ulong totalReadBytesCount = 0;
                        int readBytesCount;
                        FileSize = (UInt64)source.Length;
                        while ((readBytesCount = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (abortCopy)
                            {
                                copyResult = false;
                                break;
                            }
                            dest.Write(buffer, 0, readBytesCount);
                            totalReadBytesCount += (ulong)readBytesCount;
                            destMedia.FileSize = totalReadBytesCount;
                        }
                    }
                }
                finally
                {
                    if (_directory is IngestDirectory)
                        (_directory as IngestDirectory).LockXDCAM(false);
                    if (destMedia.Directory is IngestDirectory)
                        (destMedia.Directory as IngestDirectory).LockXDCAM(false);
                }
            }
            return copyResult;
        }
        
        public override string ToString()
        {
            return string.Format("{0}:{1}", _directory.DirectoryName, MediaName);
        }

        public virtual bool FileExists()
        {
            return File.Exists(FullPath);
        }

        public void Remove()
        {
            ((MediaDirectory)Directory).MediaRemove(this);
        }

        private bool _verified = false;
        [JsonProperty]
        public bool Verified
        {
            get { return _verified; }
            set { SetField(ref _verified, value, nameof(Verified)); }
        }

        [JsonProperty]
        public RationalNumber FrameRate { get { return VideoFormatDescription.FrameRate; } }

        public void ReVerify()
        {
            MediaStatus = TMediaStatus.Unknown;
            Verified = false;
            ThreadPool.QueueUserWorkItem((o) => Verify());
        }

        internal virtual void Verify()
        {
            if (Verified || (_mediaStatus == TMediaStatus.Copying) || (_mediaStatus == TMediaStatus.CopyPending || _mediaStatus == TMediaStatus.Required))
                return;
            if (_directory != null && System.IO.Directory.Exists(_directory.Folder) && !File.Exists(FullPath))
            {
                _mediaStatus = TMediaStatus.Deleted;
                return; // in case that no file was found, and directory exists
            }
            try
            {
                FileInfo fi = new FileInfo(FullPath);
                if (fi.Length == 0L)
                    return;
                if ((MediaType != TMediaType.Animation)
                    &&
                    (MediaStatus == TMediaStatus.Unknown
                    || MediaStatus == TMediaStatus.Deleted
                    || MediaStatus == TMediaStatus.Copied
                    || (MediaType != TMediaType.Still && Duration == TimeSpan.Zero)
                    || FileSize != (UInt64)fi.Length
                    || !LastUpdated.DateTimeEqualToDays(fi.LastWriteTimeUtc)
                    ))
                {
                    FileSize = (UInt64)fi.Length;
                    LastUpdated = DateTimeExtensions.FromFileTime(fi.LastWriteTimeUtc, DateTimeKind.Utc);
                    //this.LastAccess = DateTimeExtensions.FromFileTime(fi.LastAccessTimeUtc, DateTimeKind.Utc);

                    if (SetField(ref _mediaStatus, MediaChecker.Check(this), nameof(MediaStatus)))
                    {
                        var dir = _directory;
                        if (dir != null)
                            dir.OnMediaVerified(this);
                    }                    
                }                
                Verified = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public void GetLoudness()
        {
            _directory.MediaManager.FileManager.Queue(new LoudnessOperation() { SourceMedia = this, MeasureStart = this.TcPlay - this.TcStart, MeasureDuration = this.DurationPlay }, false);
        }
    }

}
