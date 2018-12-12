package cn.venyo;

import com.hankcs.hanlp.dictionary.CustomDictionary;
import com.hankcs.lucene.HanLPAnalyzer;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.document.Document;
import org.apache.lucene.document.Field;
import org.apache.lucene.document.StoredField;
import org.apache.lucene.document.TextField;
import org.apache.lucene.index.DirectoryReader;
import org.apache.lucene.index.IndexWriter;
import org.apache.lucene.index.IndexWriterConfig;
import org.apache.lucene.queryparser.classic.MultiFieldQueryParser;
import org.apache.lucene.search.IndexSearcher;
import org.apache.lucene.search.Query;
import org.apache.lucene.search.ScoreDoc;
import org.apache.lucene.search.Sort;
import org.apache.lucene.store.Directory;
import org.apache.lucene.store.FSDirectory;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import org.apache.lucene.document.StringField;
import org.apache.lucene.index.IndexReader;
import org.apache.lucene.index.Term;
import org.apache.lucene.search.TermQuery;

import static spark.Spark.*;

public class App
{
    static {
        try{
            Files.walk(Paths.get(App.class.getResource("/dict").toURI()))
                    .filter(Files::isRegularFile)
                    .forEach(file -> {
                        try {
                            Files.lines(Paths.get(file.toUri()))
                                    .filter(line -> !line.isEmpty())
                                    .forEach(line -> {
                                        CustomDictionary.add(line.split("\\s+")[0]);
                                    });
                        } catch (IOException e) {
                            e.printStackTrace();
                        }
                    });
        }
        catch (Exception e){
            e.printStackTrace();
        }
    }

    private static Logger logger = LoggerFactory.getLogger(App.class);

    public static void main( String[] args )
    {
        port(4685);

        get("/health", ((request, response) -> "ok"));

        post("/index", ((request, response) -> {
            try{
                String url = request.queryMap("url").value();
                String content = request.queryMap("content").value();
                String title = request.queryMap("title").value();
                String keywords = request.queryMap("keywords").value();
                String desc = request.queryMap("desc").value();
                if(url == null || url.isEmpty() || title == null || title.isEmpty()){
                    return false;
                }

                Analyzer analyzer = new HanLPAnalyzer();
                Directory directory = FSDirectory.open(Paths.get("index"));
                IndexWriterConfig config = new IndexWriterConfig(analyzer);
                IndexWriter iwriter = new IndexWriter(directory, config);
                Document doc = new Document();
                doc.add(new StringField("url", url, Field.Store.YES));
                if(content != null && !content.isEmpty()){
                    doc.add((new Field("content", content.toLowerCase(), TextField.TYPE_STORED)));
                }
                doc.add((new Field("title", title.toLowerCase(), TextField.TYPE_STORED)));
                if(keywords != null && !keywords.isEmpty()){
                    doc.add((new Field("keywords", keywords.toLowerCase(), TextField.TYPE_STORED)));
                }
                if(desc != null && !desc.isEmpty()){
                    doc.add((new Field("desc", desc.toLowerCase(), TextField.TYPE_STORED)));
                }
                iwriter.updateDocument(new Term("url", url), doc);
                iwriter.close();

                return true;
            }
            catch (Exception e){
                logger.error("throw exception in index method", e);
                return false;
            }
        }));

        get("/search", ((request, response) -> {
            List<HtmlPage> results = new ArrayList<>();

            String keyword = request.queryMap("keyword").value();
            if(keyword == null || keyword.isEmpty()){
                return results;
            }

            try{
                Analyzer analyzer = new HanLPAnalyzer();
                Directory directory = FSDirectory.open(Paths.get("index"));
                DirectoryReader ireader = DirectoryReader.open(directory);
                IndexSearcher isearcher = new IndexSearcher(ireader);
                MultiFieldQueryParser parser = new MultiFieldQueryParser(new String[]{"content", "title", "keywords", "desc"}, analyzer);
                Query query = parser.parse(keyword.toLowerCase());
                ScoreDoc[] hits = isearcher.search(query, 10, new Sort()).scoreDocs;

                for (int i = 0; i < hits.length; i++) {
                    Document hitDoc = isearcher.doc(hits[i].doc);
                    HtmlPage page = new HtmlPage();
                    page.setUrl(hitDoc.get("url"));
                    page.setTitle(hitDoc.get("title"));
                    page.setDesc(hitDoc.get("desc"));
                    results.add(page);
                }
                ireader.close();
                directory.close();

                return results;
            }
            catch (Exception e){
                logger.error("throw exception in search method", e);
                return results;
            }
        }), new JsonTransformer());
    }
}
