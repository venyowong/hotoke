package cn.venyo;

import cn.venyo.spark.JsonTransformer;
import cn.venyo.spark.Action;
import java.io.InputStream;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import static spark.Spark.*;

public class App
{
    private static Logger LOGGER = LoggerFactory.getLogger(App.class);
    
    static {
        try{
            InputStream inputStream = App.class.getClassLoader().getResourceAsStream("application.properties");
            Utility.APPLICATION_PROPERTIES.load(inputStream);
            inputStream.close();
        }
        catch (Exception e){
            e.printStackTrace();
        }
    }

    public static void main( String[] args )
    {
        ipAddress("0.0.0.0");
        port(4685);
        
        get("/health", ((request, response) -> "ok"));
        
        post("/index", Action::index);
        
        post("/:index/index", Action::index);

        get("/search", Action::search, new JsonTransformer());
        
        get("/:index/search", Action::search, new JsonTransformer());
        
        get("/count", Action::count);
        
        get("/:index/count", Action::count);
    }
}
