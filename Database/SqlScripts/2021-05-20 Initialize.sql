-- Initialize application database
-- 2021-05-20 Andreas Mueller <webmaster@am-wd.de>

create table users
(
	id integer not null,
	username text(50) not null,
	password_hash text(200),
	is_enabled integer(1) not null default 1,
	is_admin integer(1) not null default 0,
	constraint pk_users
		primary key (id),
	constraint uq_user
		unique (username)
);

create table mqtt_users
(
	id integer not null,
	username text(50) not null,
	password_hash text(200),
	client_regex text(150) not null,
	base_topic text(100),
	do_rewrite integer(1) not null default 0,
	is_enabled integer(1) not null default 1,
	constraint pk_mqtt_users
		primary key (id),
	constraint uq_mqtt_user
		unique (username)
);

create table mqtt_permissions
(
	user_id integer not null,
	topic text(200) not null,
	can_read integer(1) not null default 1,
	can_write integer(1) not null default 0,
	constraint pk_mqtt_permissions
		primary key (user_id, topic),
	constraint fk_mqtt_permission_user
		foreign key (user_id)
		references mqtt_users (id)
		on delete cascade
);
