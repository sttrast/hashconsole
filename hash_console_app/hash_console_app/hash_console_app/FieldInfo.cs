using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace TCI.BusinessLayer.BusinessObjects
{
    [Serializable]
    public class FieldInfo
    {
        private DbType _dbType;        
        private string _fValue;
        public FieldInfo() { }
        public FieldInfo(DbType dbType, string fValue)
        {
            this._dbType = dbType;
            this._fValue = fValue;

        }
        public DbType DbType
        {
            get { return _dbType; }
        }
        public string FValue
        {
            get { return _fValue; }
        }

    }
}


