function show_error(message){
	$("#error").text(message);
	$("#error").show();
}

function show_login_page(){
	$("#error").hide();
	$("#login_page").show();
	$("#main_page").hide();
}

function show_main_page(user){
	if(user){
		$("#email_label").text(user.eMail);
	}

	$("#error").hide();
	$("#login_page").hide();
	$("#main_page").show();
}

function collectBookmarks(prefix, node, bookmarks){
	if(node.url){
		bookmarks.push({path: prefix, title: node.title, url: node.url})
	}

	if(node.children && node.children.length > 0){
		if(prefix && node.title){
			var prefix = `${prefix}>${node.title}`
		}
		else if(node.title){
			var prefix = node.title
		}

		for (var i = 0; i < node.children.length; i++) {
			collectBookmarks(prefix, node.children[i], bookmarks);
		}
	}
}

document.addEventListener('DOMContentLoaded', function () {
  	$("#login_button").click(function(){
		var email = $("#email").val();
		var password = $("#password").val();
		if(!email || !password){
			show_error("invalid parameters");
		}

		$.post("https://venyo.cn/user/login",
		{email: email, password: hex_md5(password)},
		function(data){
			if(data.success){
				window.localStorage.setItem("user", JSON.stringify(data.result));
				user = data.result;
				show_main_page(user);
			}
			else{
				show_error(data.message);
			}
		})
	});

	$("#index_button").click(function(){
		$("#index_button").val("indexing...");
		$("#index_button").attr("disabled",true);
		chrome.bookmarks.getTree(
		function(bookmarkTreeNodes) {
			if(bookmarkTreeNodes && bookmarkTreeNodes.length > 0){
				var bookmarks = []
				for (var i = 0; i < bookmarkTreeNodes.length; i++) {
					collectBookmarks(null, bookmarkTreeNodes[i], bookmarks);
				}
				var indexed = 0;
				for(var i = 0; i < bookmarks.length; i++){
					$.ajax({
						type: "post",
						url: "https://venyo.cn/bookmark/upsert",
						data: {url: bookmarks[i].url},
						headers: {"access-token": user.token},
						success: function(){
							indexed++;
						},
						error: function(){
							indexed++;
						}
					});
				}

				$("#index_button").val("Index");
				$("#index_button").attr("disabled",false);
			}
		});
	});

	$("#search_button").click(function(){
		var keyword = $("#keyword")[0].value;
		if(!keyword){
			return;
		}

		$.ajax({
			type: "get",
			url: `https://venyo.cn/bookmark/search?keyword=${encodeURIComponent(keyword)}`,
			headers: {"access-token": user.token},
			success: function(data){
				if(data){
					$("#results").children().remove();
					var template = $("#result_template")[0];
					for(var i = 0; i < data.length; i++){
						var node = template.cloneNode(true);
						node.id = null;
						node.style.display = "";
						node.href = data[i].url;
						var title = node.getElementsByTagName("h5")[0];
						title.innerText = data[i].title;
						var desc = node.getElementsByTagName("p")[0];
						desc.innerText = data[i].desc;
						var url = node.getElementsByTagName("small")[0];
						url.innerText = data[i].url;
						$("#results").append(node);
					}
				}
			},
			error: function(data){
				if(data.status == 401){
					show_login_page();
				}
			}
		});
	});

	user = window.localStorage.getItem("user");
	try{
		user = JSON.parse(user);
	}
	catch{
		user = null;
	}
	if(user){
		$.ajax({
			type: "get",
			url: 'https://venyo.cn/user/isvalidtoken',
			headers: {"access-token": user.token},
			success: function(){
				show_main_page(user);
			},
			error: show_login_page
		});
	}
	else{
		show_login_page();
	}
});
