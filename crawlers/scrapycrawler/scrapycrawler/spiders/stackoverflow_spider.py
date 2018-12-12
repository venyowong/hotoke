#!/usr/bin/env python
# -*- coding: utf-8 -*-

import logging

import scrapy
from scrapycrawler.items import StackOverflowItem
import requests
from scrapycrawler.config import INDEX_HOST


formatter = logging.Formatter(
    '%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger('monitor')
logger.setLevel(logging.INFO)

fh = logging.FileHandler('monitor.log')
fh.setLevel(logging.INFO)

fh.setFormatter(formatter)
logger.addHandler(fh)


class StackOverflowSpider(scrapy.Spider):

    name = "stackoverflow"

    def __init__(self):
        self.count = 1

    def start_requests(self):
        _url = 'https://stackoverflow.com/questions?page={page}&sort=votes&pagesize=50'
        urls = [_url.format(page=page) for page in range(1, 200001)]
        for url in urls:
            yield scrapy.Request(url=url, callback=self.parse)

    def parse(self, response):
        for index in range(1, 51):
            self.count += 1
            if self.count % 100 == 0:
                logger.info(self.count)

            sel = response.xpath('//*[@id="questions"]/div[{index}]'.format(index=index))
            item = StackOverflowItem()
            item['votes'] = sel.xpath(
                'div[1]/div[1]/div[1]/div[1]/span/strong/text()').extract()[0]
            item['desc'] = sel.xpath(
                'div[2]/div[1]/text()').extract()[0].strip()
            item['views'] = "".join(
                sel.xpath('div[1]/div[2]/@title').extract()).split()[0].replace(",", "")
            item['title'] = sel.xpath('div[2]/h3/a/text()').extract()[0]
            item['url'] = "https://stackoverflow.com/questions/{}".format(
                sel.xpath('div[2]/h3/a/@href').extract()[0].split("/")[2])
            item['keywords'] = ",".join(sel.xpath('div[2]/div[2]/a/text()').extract())
            
            requests.post("http://{}/index".format(INDEX_HOST), data=item)
