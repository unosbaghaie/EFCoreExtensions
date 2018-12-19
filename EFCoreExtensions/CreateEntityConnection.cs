using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace EFCoreExtensions
{
    internal class CreateEntityConnection : DbConnection
    {
        public CreateEntityConnection(DbConnection originalConnection, DbDataReader originalDataReader)
        {
            OriginalConnection = originalConnection;
            OriginalDataReader = originalDataReader;
        }

        private DbConnection OriginalConnection { get; }

        internal DbDataReader OriginalDataReader { get; set; }

        public override string ConnectionString
        {
            get { return OriginalConnection.ConnectionString; }
            set { OriginalConnection.ConnectionString = value; }
        }

        public override string Database
        {
            get { return OriginalConnection.Database; }
        }

        public override string DataSource
        {
            get { return OriginalConnection.DataSource; }
        }

        public override string ServerVersion
        {
            get { return OriginalConnection.ServerVersion; }
        }

        public override ConnectionState State
        {
            get { return OriginalConnection.State; }
        }

        public override void ChangeDatabase(string databaseName)
        {
            OriginalConnection.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
            OriginalConnection.Close();
        }

        public override void Open()
        {
            OriginalConnection.Open();
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return OriginalConnection.BeginTransaction();
        }

        protected override DbCommand CreateDbCommand()
        {
            return new CreateEntityCommand(OriginalConnection.CreateCommand(), OriginalDataReader);
        }
    }
}
