using LuceneDemo.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuceneDemo
{
   public class SQLRecords
    {
       LuceneDemoDataContext recordsContext = new LuceneDemoDataContext();
        public List<lucene1> GetAllRecords()
        {
            return recordsContext.lucene1s.ToList();
        }
    }
}
