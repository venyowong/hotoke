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
import cn.venyo.index.CustomIndex;
import cn.venyo.index.IndexManager;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import org.ansj.lucene7.AnsjAnalyzer;
import org.ansj.lucene7.AnsjAnalyzer.TYPE;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.document.Document;
import org.apache.lucene.document.Field;
import org.apache.lucene.document.StringField;
import org.apache.lucene.document.TextField;
import org.apache.lucene.index.DirectoryReader;
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
    private static final String[] FIELDS = new String[]{"content", "title", "keywords", "desc"};
    private static final Map<String, Float> BOOSTS = new HashMap<String, Float>();
    
    static {
        BOOSTS.put("content", 0.3f);
        BOOSTS.put("title", 1.5f);
        BOOSTS.put("keywords", 1.3f);
        BOOSTS.put("desc", 0.5f);
    }
    
    public static Object index(Request request, Response response){
        try{
            String indexName = request.params(":index");
            String url = request.queryMap("url").value();
            String content = request.queryMap("content").value();
            String title = request.queryMap("title").value();
            String keywords = request.queryMap("keywords").value();
            String desc = request.queryMap("desc").value();
            if(url == null || url.isEmpty() || title == null || title.isEmpty()){
                return false;
            }

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
            if(indexName != null && !indexName.isEmpty()){
                CustomIndex index = IndexManager.getIndex(indexName);
                index.getIndexWriter().updateDocument(new Term("id", id), doc);
                index.getIndexWriter().commit();
                index.release();
            }
            else{
                IndexManager.INDEX_WRITER.updateDocument(new Term("id", id), doc);
            }

            LOGGER.info("indexed: " + url);
            return true;
        }
        catch (Exception e){
            LOGGER.error("throw exception in index method", e);
            return false;
        }
    }
    
    
    public static Object search(Request request, Response response){
        List<HtmlPage> results = new ArrayList<>();

        String keyword = request.queryMap("keyword").value();
        if(keyword == null || keyword.isEmpty()){
            return results;
        }

        try{
            String indexName = request.params(":index");
            Analyzer analyzer = new AnsjAnalyzer(TYPE.index_ansj);
            Directory directory = indexName != null && !indexName.isEmpty() ? 
                    FSDirectory.open(Paths.get("custom/" + indexName)): FSDirectory.open(Paths.get("index"));
            DirectoryReader ireader = DirectoryReader.open(directory);
            IndexSearcher isearcher = new IndexSearcher(ireader);
            MultiFieldQueryParser parser = new MultiFieldQueryParser(FIELDS, analyzer, BOOSTS);
            Query query = parser.parse(keyword.toLowerCase());
            ScoreDoc[] hits = isearcher.search(query, 5, new Sort()).scoreDocs;

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
            LOGGER.error("throw exception in search method", e);
            return results;
        }
    }
    
    public static int count(Request request, Response response){
        try{
            String indexName = request.params(":index");
            Directory directory = indexName != null && !indexName.isEmpty() ? 
                    FSDirectory.open(Paths.get("custom/" + indexName)): FSDirectory.open(Paths.get("index"));
            DirectoryReader ireader = DirectoryReader.open(directory);
            int result = ireader.maxDoc();
            ireader.close();
            directory.close();
            return result;
        }
        catch(Exception e){
            LOGGER.error("throw exception in search method", e);
            return 0;
        }
    }
}
