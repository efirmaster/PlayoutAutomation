﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.ComponentModel;
using MySql.Data.MySqlClient;

namespace TAS.Server.Database
{
    [DesignerCategory("Code")]
    public class DbCommandRedundant : DbCommand
    {
        private readonly MySqlCommand _commandPrimary;
        private readonly MySqlCommand _commandSecondary;
        #region Constructors

        public DbCommandRedundant()
        {
            _commandPrimary = new MySqlCommand();
            _commandSecondary = new MySqlCommand();
        }

        public DbCommandRedundant(string commandText)
        {
            _commandPrimary = new MySqlCommand(commandText);
            _commandSecondary = new MySqlCommand(commandText);
        }

        public DbCommandRedundant(string commandText, DbConnectionRedundant connection)
        {
            _commandPrimary = new MySqlCommand(commandText, connection.ConnectionPrimary);
            _commandSecondary = new MySqlCommand(commandText, connection.ConnectionSecondary);
        }

        internal DbCommandRedundant(DbConnectionRedundant connection)
        {
            _commandPrimary = new MySqlCommand() { Connection = connection.ConnectionPrimary };
            _commandSecondary = new MySqlCommand() { Connection = connection.ConnectionSecondary };
        }

        #endregion //Constructors


        public new void Dispose()
        {
            _commandPrimary.Dispose();
            _commandSecondary.Dispose();
        }

        private string _commandText;
        public override string CommandText
        {
            get { return _commandText; }
            set
            {
                if (_commandText != value)
                {
                    _commandText = value;
                    _commandPrimary.CommandText = value;
                    _commandSecondary.CommandText = value;
                }
            }
        }

        private int _commandTimeout;
        public override int CommandTimeout
        {
            get { return _commandTimeout; }
            set
            {
                if (_commandTimeout != value)
                {
                    _commandTimeout = value;
                    _commandPrimary.CommandTimeout = value;
                    _commandSecondary.CommandTimeout = value;
                }
            }
        }

        private CommandType _commandType;
        public override CommandType CommandType
        {
            get { return _commandType; }
            set
            {
                if (_commandType != value)
                {
                    _commandType = value;
                    _commandPrimary.CommandType = value;
                    _commandSecondary.CommandType = value;
                }
            }
        }

        private bool _designTimeVisible;
        public override bool DesignTimeVisible
        {
            get { return _designTimeVisible; }
            set
            {
                if (_designTimeVisible != value)
                {
                    _designTimeVisible = value;
                    _commandPrimary.DesignTimeVisible = value;
                    _commandSecondary.DesignTimeVisible = value;
                }
            }
        }

        private UpdateRowSource _updatedRowSource;
        public override UpdateRowSource UpdatedRowSource
        {
            get { return _updatedRowSource; }
            set
            {
                if (_updatedRowSource != value)
                {
                    _updatedRowSource = value;
                    _commandPrimary.UpdatedRowSource = value;
                    _commandSecondary.UpdatedRowSource = value;
                }
            }
        }

        private DbConnection _dbConnection;
        protected override DbConnection DbConnection
        {
            get { return _dbConnection; }
            set
            {
                if (_dbConnection != value && value is DbConnectionRedundant)
                {
                    _dbConnection = value;
                    _commandPrimary.Connection = (value as DbConnectionRedundant).ConnectionPrimary;
                    _commandSecondary.Connection = (value as DbConnectionRedundant).ConnectionSecondary;
                }
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private DbParameterCollectionRedundant _parameters;
        public new DbParameterCollectionRedundant Parameters {
            get
            {
                if (_parameters == null)
                    _parameters = new DbParameterCollectionRedundant();
                return _parameters;
            }
        }

        protected override DbTransaction DbTransaction { get; set; }

        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        private void _fillParameters()
        {
            if (_parameters != null)
                foreach (var parameter in _parameters)
                {
                    _commandPrimary.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    _commandSecondary.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
        }

        public override int ExecuteNonQuery()
        {
            _fillParameters();
            int ret1 = _commandPrimary.ExecuteNonQuery();
            int ret2 = _commandSecondary.ExecuteNonQuery();
            return ret1;
        }

        public override object ExecuteScalar()
        {
            _fillParameters();
            object ret1 = _commandPrimary.ExecuteScalar();
            object ret2 = _commandSecondary.ExecuteScalar();
            return ret1;
        }

        public override void Prepare()
        {
            _fillParameters();
            _commandPrimary.Prepare();
            _commandSecondary.Prepare();
        }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            throw new NotImplementedException();
        }

        public new DbDataReaderRedundant ExecuteReader(CommandBehavior behavior)
        {
            _fillParameters();
            return new DbDataReaderRedundant(this, behavior);
        }

        public new DbDataReaderRedundant ExecuteReader()
        {
            return ExecuteReader(CommandBehavior.Default);
        }

        internal MySqlCommand CommandPrimary { get { return _commandPrimary; } }
        internal MySqlCommand CommandSecondary { get { return _commandSecondary; } }

        public long LastInsertedId
        {
            get
            {
                return _commandPrimary.LastInsertedId;
            }
        }
    }
}
