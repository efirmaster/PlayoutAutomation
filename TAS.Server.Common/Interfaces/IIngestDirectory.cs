﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IIngestDirectory: IMediaDirectory, IIngestDirectoryConfig
    {
        TDirectoryAccessType AccessType { get; }
        System.Net.NetworkCredential NetworkCredential { get; }
        string Filter { get; set; }
    }
}
