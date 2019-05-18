#!/bin/sh

curl -XDELETE http://localhost:9200/bookmark

curl -XPUT http://localhost:9200/bookmark/ -d '
{
	"mappings":{
		"bookmark":{
			"properties":{
				"url": {
					"type": "keyword"
				},
				"title":{
					"type": "text",
					"analyzer" : "ik_max_word",
                    "search_analyzer": "ik_max_word",
					"fielddata": true
				},
				"keywords":{
					"type": "text",
					"analyzer" : "ik_max_word",
                    "search_analyzer": "ik_max_word",
					"fielddata": true
				},
				"description":{
					"type": "text",
					"analyzer" : "ik_max_word",
                    "search_analyzer": "ik_max_word",
					"fielddata": true
				},
				"content":{
					"type": "text",
					"analyzer" : "ik_max_word",
                    "search_analyzer": "ik_max_word",
					"fielddata": true
				},
                "user_id":{
                    "type": "keyword"
                },
                "path":{
                    "type": "keyword"
                },
				"remark":{
					"type": "text",
					"analyzer" : "ik_max_word",
                    "search_analyzer": "ik_max_word",
					"fielddata": true
				}
			}
		}
	}
}'
