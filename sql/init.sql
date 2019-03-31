drop database if exists hotoke;
create database hotoke;
use hotoke;
create table user(
    id int auto_increment not null primary key,
    email varchar(50) not null,
    password varchar(50) not null,
    salt varchar(50) not null
);