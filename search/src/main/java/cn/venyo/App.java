package cn.venyo;

import cn.venyo.spark.JsonTransformer;
import brave.Tracing;
import brave.opentracing.BraveTracer;
import cn.venyo.spark.Action;
import cn.venyo.spark.Filter;
import java.io.BufferedReader;
import java.io.FileInputStream;
import java.io.InputStream;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.InputStreamReader;
import java.net.URL;
import java.util.stream.Collectors;

import static spark.Spark.*;
import zipkin2.Span;
import zipkin2.reporter.AsyncReporter;
import zipkin2.reporter.okhttp3.OkHttpSender;

public class App
{
    private static Logger LOGGER = LoggerFactory.getLogger(App.class);
    
    static {
        try{
            InputStream inputStream = App.class.getClassLoader().getResourceAsStream("application.properties");
            Utility.APPLICATION_PROPERTIES.load(inputStream);
            inputStream.close();
            
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
        ipAddress("0.0.0.0");
        port(4685);
        
        before(Filter::before);

        get("/health", ((request, response) -> "ok"));
        
        post("/index", Action::index);

        get("/search", Action::search, new JsonTransformer());
        
        get("/count", Action::count);
    }
}
