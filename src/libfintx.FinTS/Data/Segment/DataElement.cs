using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace libfintx.FinTS.Data.Segment
{
    public class DataElement
    {
        public string Value { get; set; }

        public string EscapedValue => Value != null ? Regex.Replace(Value, @"\?(\+|:|'|\?|@)", "$1") : null;

        private List<DataElement> _dataElements;
        /// <summary>
        /// Datenelemente der Payload.
        /// </summary>
        public List<DataElement> DataElements
        {
            get
            {
                if (_dataElements == null)
                    _dataElements = new List<DataElement>();

                return _dataElements;
            }
            set
            {
                _dataElements = value;
            }
        }

        public bool IsDataElementGroup => _dataElements?.Count > 0;

        public DataElement()
        {
        }

        public DataElement(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return IsDataElementGroup ? string.Join(" ", DataElements) : Value;
        }
    }
}
