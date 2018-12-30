/*
 * Copyright 2018 venyowong<https://github.com/venyowong>.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package cn.venyo.spark;

import cn.venyo.HtmlPage;
import cn.venyo.Utility;
import com.hankcs.lucene.HanLPAnalyzer;
import io.opentracing.Span;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.List;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.document.Document;
import org.apache.lucene.document.Field;
import org.apache.lucene.document.StringField;
import org.apache.lucene.document.TextField;
import org.apache.lucene.index.DirectoryReader;
import org.apache.lucene.index.IndexWriter;
import org.apache.lucene.index.IndexWriterConfig;
import org.apache.lucene.index.Term;
import org.apache.lucene.queryparser.classic.MultiFieldQueryParser;
import org.apache.lucene.search.IndexSearcher;
import org.apache.lucene.search.Query;
import org.apache.lucene.search.ScoreDoc;
import org.apache.lucene.search.Sort;
import org.apache.lucene.store.Directory;
import org.apache.lucene.store.FSDirectory;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import spark.Request;
import spark.Response;

/**
 *
 * @author venyowong<https://github.com/venyowong>
 */
public class Action {
    private static Logger LOGGER = LoggerFactory.getLogger(Action.class);
    
    public static Object index(Request request, Response response){
        Span span = Utility.TRACER.buildSpan("index")
            .start();

        try{
            String url = request.queryMap("url").value();
            String content = request.queryMap("content").value();
            String title = request.queryMap("title").value();
            String keywords = request.queryMap("keywords").value();
            String desc = request.queryMap("desc").value();
            if(url == null || url.isEmpty() || title == null || title.isEmpty()){
                span.setTag("status", "invalid paramters");
                span.finish();
                return false;
            }
            span.log("got all parameters");

            Analyzer analyzer = new HanLPAnalyzer();
            Directory directory = FSDirectory.open(Paths.get("index"));
            IndexWriterConfig config = new IndexWriterConfig(analyzer);
            IndexWriter iwriter = new IndexWriter(directory, config);
            Document doc = new Document();
            String id = String.valueOf(url.hashCode());
            doc.add(new StringField("id", id, Field.Store.YES));
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
            span.log("created document");
            iwriter.updateDocument(new Term("id", id), doc);
            iwriter.close();

            span.setTag("status", "success");
            span.finish();
            return true;
        }
        catch (Exception e){
            LOGGER.error("throw exception in index method", e);
            String message = e.getMessage();
            if(message != null && !message.isEmpty()){
                span.log(message);
            }
            span.setTag("status", "catched exception");
            span.finish();
            return false;
        }
    }
    
    public static Object search(Request request, Response response){
        Span span = Utility.TRACER.buildSpan("index")
            .start();
        List<HtmlPage> results = new ArrayList<>();

        String keyword = request.queryMap("keyword").value();
        if(keyword == null || keyword.isEmpty()){
            span.setTag("status", "invalid paramters");
            span.finish();
            return results;
        }

        try{
            Analyzer analyzer = new HanLPAnalyzer();
            Directory directory = FSDirectory.open(Paths.get("index"));
            DirectoryReader ireader = DirectoryReader.open(directory);
            IndexSearcher isearcher = new IndexSearcher(ireader);
            MultiFieldQueryParser parser = new MultiFieldQueryParser(new String[]{"content", "title", "keywords", "desc"}, analyzer);
            Query query = parser.parse(keyword.toLowerCase());
            span.log("parsed query");
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

            span.setTag("status", "success");
            span.finish();
            return results;
        }
        catch (Exception e){
            LOGGER.error("throw exception in search method", e);
            String message = e.getMessage();
            if(message != null && !message.isEmpty()){
                span.log(message);
            }
            span.setTag("status", "catched exception");
            span.finish();
            return results;
        }
    }
}
