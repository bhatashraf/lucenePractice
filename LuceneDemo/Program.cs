using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net;
using Lucene.Net.Store;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using System.IO;
using LuceneDemo.data;
using System.Runtime.Remoting.Contexts;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace LuceneDemo
{
    class Program
    {
      
        private static string _luceneDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lucene_index");
        private static FSDirectory _directoryTemp;
        private static FSDirectory _directory
        {
            // returns the directory where indexes will be stored
            get
            {
                if (_directoryTemp == null)
                {
                    _directoryTemp = FSDirectory.Open(new DirectoryInfo(_luceneDir));
                }
                if (IndexWriter.IsLocked(_directoryTemp))
                {
                    IndexWriter.Unlock(_directoryTemp);
                }
                var lockFilePath = Path.Combine(_luceneDir, "write.lock");

                if (File.Exists(lockFilePath))
                {
                    File.Delete(lockFilePath);
                }
                return _directoryTemp;
            }
        }
       
        static void Main(string[] args)
        {
            // Get the indexes files from the "lucene_Index" folder
            string[] filePaths = System.IO.Directory.GetFiles(_luceneDir);

            // Delete all the indexes from "lucene_Index" folder
            foreach (string filePath in filePaths)
            {
                File.Delete(filePath);
            }

            #region Indexing
            //Create Directory for Indexes
            //There are 2 options, FS or RAM
            //Step 1: Declare Index Store


            //Now we need Analyzer
            //An Analyzer builds TokenStreams, which analyze text. It thus represents a policy for extracting index terms from text.
            //In general, any analyzer in Lucene is tokenizer + stemmer + stop-words filter.   
            //Tokenizer splits your text into chunks-For example, for phrase "I am very happy" it will produce list ["i", "am", "very", "happy"] 
            // stemmer:-piece of code responsible for “normalizing” words to their common form (horses => horse, indexing => index, etc)
            //Stop words are the most frequent and almost useless words
            Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

            //Need an Index Writer to write the output of Analysis to Index
            IndexWriter writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

            //Provide Documents/Build Documents
            //Add Documents to the Index.
            SQLRecords allRecords = new SQLRecords();
            List<lucene1> records = allRecords.GetAllRecords();
            foreach (lucene1 item in records)
            {
                Document doc = new Document();
                doc.Add(new Field("Id", item.Id.ToString(), Field.Store.YES, Field.Index.NO));
                doc.Add(new Field("Title", item.Title.ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field("ShortDescription", item.ShortDescription.ToLower(), Field.Store.YES, Field.Index.ANALYZED));
               // doc.Add(new Field("PageBody", item.PageBody.ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field("Date", item.DateCrated.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));

                writer.AddDocument(doc);
            }

            //Run the IndexWriter

            writer.Optimize();
            writer.Commit();
            writer.Dispose();
            //Index is ready for searching
            #endregion

            #region Searching

            // Create the Query.
            

            String[] fields = new String[] { "Title", "ShortDescription", "PageBody", "DateCreated" };

            //  Boosting/Scoring:  is the ability to assign higher importance to specific words in a query
            //Document level boosting - while indexing - by calling document.setBoost() before a document is added to the index.
            //Document's Field level boosting - while indexing - by calling field.setBoost() before adding a field to the document (and before adding the document to the index).
            //Query level boosting - during search, by setting a boost on a query clause, calling Query.setBoost().
            
            //Query level boosting used here...
            
            HashMap<String, float> boosts = new HashMap<String, float>();

            boosts.Add("Title", 15);
            boosts.Add("ShortDescription", 10);
            boosts.Add("PageBody", 5);
           

            MultiFieldQueryParser parser = new MultiFieldQueryParser(
                                        Lucene.Net.Util.Version.LUCENE_30,
                                        fields,
                                        analyzer,
                                        boosts
                                        );
            //QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "Title", analyzer);

            Console.WriteLine("Please Enter the string to be searched :\n");
            string searchItem = Console.ReadLine();
            Query query = parser.Parse(searchItem);

           

            //
            // Pass the Query to the IndexSearcher.
            //
            IndexSearcher searcher = new IndexSearcher(_directory, readOnly: true);
            TopDocs hits = searcher.Search(query, 200);

  

            Sort sort = new Sort(new SortField("DateCreated", SortField.LONG, true));
            var filter = new QueryWrapperFilter(query);
            TopDocs results = searcher.Search(query, filter , 1000, sort);

            
            int result = results.ScoreDocs.Length;


            Console.WriteLine("Found {0} results", result);

          
            foreach (var item in results.ScoreDocs)
            {
                Document document = searcher.Doc(item.Doc);


                foreach (var i in document.fields_ForNUnit)
                    {
                    if (i.StringValue.Contains(searchItem))
                        {
                            Console.WriteLine("ID: {0}", document.Get("Id"));
                            Console.WriteLine("Text found: {0}" + Environment.NewLine, i.StringValue);
                            Console.WriteLine("Date Created: {0}" + Environment.NewLine, document.Get("DateCreated"));
                        }
                    }                
            }

            Console.WriteLine("Press ENTER to quit...");
            Console.ReadLine();            
            #endregion
        }        
    }
}
