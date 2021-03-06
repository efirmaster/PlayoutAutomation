﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using MySql.Data.MySqlClient;

namespace TAS.Client.Config
{
    public class ConnectionStringViewmodel : OkCancelViewmodelBase<MySqlConnectionStringBuilder>
    {
        private string _database;
        private uint _port;
        private string _server;
        private string _userId;
        private string _password;
        private string _characterSet;
        private IEnumerable<string> _characterSets = new List<string>() { "utf8" };
        public ConnectionStringViewmodel(string connectionString) : base(new MySqlConnectionStringBuilder() { ConnectionString = connectionString }, new ConnectionStringView(), "Edit connection parameters") { }

        protected override void OnDispose() { }

        public string ConnectionString { get { return Model.ConnectionString; } }
        public string Server { get { return _server; } set { SetField(ref _server, value, nameof(Server)); } }
        public uint Port { get { return _port; } set { SetField(ref _port, value, nameof(Port)); } }
        public string Database { get { return _database; } set { SetField(ref _database, value, nameof(Database)); } }
        public string UserID { get { return _userId; } set { SetField (ref _userId, value, nameof(UserID));} }
        public string Password { get { return _password; } set { SetField(ref _password, value, nameof(Password)); } }
        public string CharacterSet { get { return _characterSet; } set { SetField(ref _characterSet, value, nameof(CharacterSet)); } }
        public IEnumerable<string> CharacterSets { get { return _characterSets; } }
    }
}
