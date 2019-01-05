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
package cn.venyo.index;

import cn.venyo.Utility;
import java.io.IOException;
import java.nio.file.Paths;
import java.util.logging.Level;
import java.util.logging.Logger;
import org.ansj.lucene7.AnsjAnalyzer;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.index.IndexWriter;
import org.apache.lucene.index.IndexWriterConfig;
import org.apache.lucene.store.Directory;
import org.apache.lucene.store.FSDirectory;

/**
 *
 * @author venyowong<https://github.com/venyowong>
 */
public class IndexManager {
    private static Thread COMMIT_THREAD;
    public static IndexWriter INDEX_WRITER;
    
    static {
        Analyzer analyzer = new AnsjAnalyzer(AnsjAnalyzer.TYPE.nlp_ansj);
        try {
            Directory directory = FSDirectory.open(Paths.get("index"));
            IndexWriterConfig config = new IndexWriterConfig(analyzer);
            INDEX_WRITER = new IndexWriter(directory, config);
            
            COMMIT_THREAD = new Thread(){
                @Override
                public void run() {
                    while(INDEX_WRITER.isOpen()){
                        try {
                            String interval = Utility.APPLICATION_PROPERTIES.getProperty("commit.interval");
                            Thread.sleep(Long.parseLong(interval));
                            INDEX_WRITER.commit();
                        } catch (InterruptedException ex) {
                            Logger.getLogger(IndexManager.class.getName()).log(Level.SEVERE, null, ex);
                        } catch (IOException ex) {
                            Logger.getLogger(IndexManager.class.getName()).log(Level.SEVERE, null, ex);
                        }
                    }
                }
            };
            COMMIT_THREAD.start();
        } catch (IOException ex) {
            Logger.getLogger(IndexManager.class.getName()).log(Level.SEVERE, null, ex);
        }
    }
}
