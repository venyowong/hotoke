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

import cn.venyo.Utility;
import io.opentracing.Scope;
import io.opentracing.SpanContext;
import io.opentracing.contrib.web.servlet.filter.HttpServletRequestExtractAdapter;
import io.opentracing.propagation.Format;
import spark.Request;
import spark.Response;

/**
 *
 * @author venyowong<https://github.com/venyowong>
 */
public class Filter {
    public static void before(Request request, Response response){
        SpanContext spanContext = Utility.TRACER.extract(Format.Builtin.HTTP_HEADERS, new HttpServletRequestExtractAdapter(request.raw()));
        Scope scope = Utility.TRACER.buildSpan("spark request")
            .asChildOf(spanContext)
            .startActive(true);
        request.attribute("scope", scope);
    }
    
    public static void after(Request request, Response response){
        Scope scope = request.attribute("scope");
        if(scope == null){
            return;
        }
        
        scope.close();
    }
}
