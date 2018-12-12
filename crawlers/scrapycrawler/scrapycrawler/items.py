# -*- coding: utf-8 -*-

# Define here the models for your scraped items
#
# See documentation in:
# https://doc.scrapy.org/en/latest/topics/items.html

import scrapy


class StackOverflowItem(scrapy.Item):
    """
    StackOverflow 页面实体
    """
    url = scrapy.Field()
    views = scrapy.Field()
    votes = scrapy.Field()
    desc = scrapy.Field()
    keywords = scrapy.Field()
    title = scrapy.Field()
    pass
