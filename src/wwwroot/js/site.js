// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function getParameter(name) 
{ 
    var ref=window.location.href;
    var args = ref.split("?"); 
    var retval = ""; 
    if(args[0] == ref) { 
        return retval; 
    } 
    var str = args[1]; 
    args = str.split("&"); 
    for(var i = 0; i < args.length; i++ ) { 
        str = args[i]; 
        var arg = str.split("="); 
        if(arg.length <= 1) continue; 
        if(arg[0] == name) retval = arg[1]; 
    } 
    return retval; 
}