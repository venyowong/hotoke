package cn.venyo;

import cn.venyo.spark.JsonTransformer;
import brave.Tracing;
import brave.opentracing.BraveTracer;
import cn.venyo.spark.Action;
import cn.venyo.spark.Filter;
import com.hankcs.hanlp.dictionary.CustomDictionary;
import java.io.FileInputStream;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Paths;

import static spark.Spark.*;
import zipkin2.Span;
import zipkin2.reporter.AsyncReporter;
import zipkin2.reporter.okhttp3.OkHttpSender;

public class App
{
    private static Logger LOGGER = LoggerFactory.getLogger(App.class);
    
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
            
            FileInputStream in = new FileInputStream(App.class.getResource("/application.properties").getFile());
            Utility.APPLICATION_PROPERTIES.load(in);
            in.close();
            
            OkHttpSender sender = OkHttpSender.create(Utility.APPLICATION_PROPERTIES.getProperty("zipkin") + "/api/v2/spans");
            AsyncReporter<Span> spanReporter = AsyncReporter.create(sender);
            Tracing tracing = Tracing.newBuilder()
                .localServiceName("hotoke-search")
                .spanReporter(spanReporter)
                .build();
            Utility.TRACER = BraveTracer.create(tracing);
        }
        catch (Exception e){
            e.printStackTrace();
        }
    }

    public static void main( String[] args )
    {
        port(4685);
        
        before(Filter::before);

        get("/health", ((request, response) -> "ok"));
        
        post("/index", Action::index);

        get("/search", Action::search, new JsonTransformer());
    }
}
