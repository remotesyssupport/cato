CREATE TABLE `clouds` (
  `cloud_id` varchar(36) NOT NULL,
  `provider` varchar(32) NOT NULL,
  `cloud_name` varchar(32) NOT NULL,
  `api_url` varchar(512) NOT NULL,
  PRIMARY KEY (`cloud_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `users` (
  `user_id` varchar(36) NOT NULL DEFAULT '',
  `username` varchar(128) NOT NULL,
  `full_name` varchar(255) NOT NULL DEFAULT '',
  `status` int(11) NOT NULL,
  `authentication_type` varchar(16) NOT NULL,
  `user_password` varchar(255) DEFAULT '1753-01-01 00:00:00',
  `expiration_dt` datetime DEFAULT NULL,
  `security_question` varchar(255) DEFAULT NULL,
  `security_answer` varchar(255) DEFAULT NULL,
  `last_login_dt` datetime DEFAULT NULL,
  `failed_login_attempts` int(11) DEFAULT NULL,
  `force_change` int(11) DEFAULT NULL,
  `email` varchar(255) DEFAULT '',
  `settings_xml` text,
  `user_role` varchar(32) NOT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `users_IX_users` (`username`(64))
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `action_plan` (
  `plan_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `task_id` varchar(36) NOT NULL,
  `run_on_dt` datetime NOT NULL,
  `action_id` varchar(36) DEFAULT NULL,
  `ecosystem_id` varchar(36) DEFAULT NULL,
  `account_id` varchar(36) DEFAULT NULL,
  `parameter_xml` text,
  `debug_level` int(11) DEFAULT NULL,
  `source` varchar(16) NOT NULL DEFAULT 'manual',
  `schedule_id` varchar(36) DEFAULT NULL,
  PRIMARY KEY (`plan_id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `action_plan_history` (
  `plan_id` bigint(20) NOT NULL,
  `task_id` varchar(36) NOT NULL,
  `run_on_dt` datetime NOT NULL,
  `action_id` varchar(36) DEFAULT NULL,
  `ecosystem_id` varchar(36) DEFAULT NULL,
  `account_id` varchar(36) DEFAULT NULL,
  `parameter_xml` text,
  `debug_level` int(11) DEFAULT NULL,
  `source` varchar(16) NOT NULL DEFAULT 'manual',
  `schedule_id` varchar(36) DEFAULT NULL,
  `task_instance` bigint(20) DEFAULT NULL,
  PRIMARY KEY (`plan_id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `action_schedule` (
  `schedule_id` varchar(36) NOT NULL DEFAULT '',
  `task_id` varchar(36) NOT NULL DEFAULT '',
  `action_id` varchar(36) DEFAULT NULL,
  `ecosystem_id` varchar(36) DEFAULT NULL,
  `account_id` varchar(36) DEFAULT NULL,
  `months` varchar(27) DEFAULT NULL,
  `days_or_weeks` int(11) DEFAULT NULL,
  `days` varchar(84) DEFAULT NULL,
  `hours` varchar(62) DEFAULT NULL,
  `minutes` varchar(172) DEFAULT NULL,
  `parameter_xml` text,
  `debug_level` int(11) DEFAULT NULL,
  `label` varchar(64) DEFAULT NULL,
  `descr` varchar(512) DEFAULT NULL,
  PRIMARY KEY (`schedule_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `application_registry` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `app_name` varchar(255) NOT NULL DEFAULT '',
  `app_instance` varchar(255) NOT NULL DEFAULT '',
  `master` tinyint(1) NOT NULL,
  `heartbeat` datetime DEFAULT NULL,
  `last_processed_dt` datetime DEFAULT NULL,
  `logfile_name` varchar(255) DEFAULT NULL,
  `load_value` decimal(18,3) DEFAULT NULL,
  `hostname` varchar(255) DEFAULT '',
  `userid` varchar(255) DEFAULT '',
  `pid` int(11) DEFAULT NULL,
  `executible_path` varchar(1024) DEFAULT '',
  `command_line` varchar(255) DEFAULT '',
  `platform` varchar(255) DEFAULT '',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `asset` (
  `asset_id` varchar(36) NOT NULL DEFAULT '',
  `asset_name` varchar(255) NOT NULL DEFAULT '',
  `asset_status` varchar(32) NOT NULL,
  `is_connection_system` int(11) NOT NULL,
  `address` varchar(255) DEFAULT '',
  `port` varchar(128) DEFAULT NULL,
  `db_name` varchar(128) DEFAULT NULL,
  `connection_type` varchar(32) DEFAULT NULL,
  `credential_id` varchar(36) DEFAULT NULL,
  `conn_string` text,
  PRIMARY KEY (`asset_id`),
  UNIQUE KEY `asset_IX_asset` (`asset_name`(64))
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `asset_credential` (
  `credential_id` varchar(36) NOT NULL,
  `username` varchar(128) NOT NULL,
  `password` varchar(2048) NOT NULL,
  `domain` varchar(128) DEFAULT NULL,
  `shared_or_local` int(11) NOT NULL,
  `shared_cred_desc` varchar(256) DEFAULT NULL,
  `privileged_password` varchar(512) DEFAULT NULL,
  `credential_name` varchar(64) NOT NULL,
  PRIMARY KEY (`credential_id`),
  UNIQUE KEY `credential_name_UNIQUE` (`credential_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cloud_account` (
  `account_id` varchar(36) NOT NULL,
  `account_name` varchar(64) NOT NULL,
  `account_number` varchar(64) DEFAULT NULL,
  `provider` varchar(16) DEFAULT NULL,
  `login_id` varchar(64) NOT NULL,
  `login_password` varchar(512) NOT NULL,
  `is_default` int(11) NOT NULL,
  `auto_manage_security` int(11) DEFAULT '1',
  PRIMARY KEY (`account_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cloud_account_keypair` (
  `keypair_id` varchar(36) NOT NULL,
  `account_id` varchar(36) NOT NULL,
  `keypair_name` varchar(512) NOT NULL,
  `private_key` varchar(4096) NOT NULL,
  `passphrase` varchar(128) DEFAULT NULL,
  PRIMARY KEY (`account_id`,`keypair_name`),
  UNIQUE KEY `keypair_id_UNIQUE` (`keypair_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ecosystem` (
  `ecosystem_id` varchar(36) NOT NULL,
  `ecosystem_name` varchar(64) NOT NULL,
  `account_id` varchar(36) NOT NULL,
  `ecotemplate_id` varchar(36) NOT NULL,
  `ecosystem_desc` varchar(512) DEFAULT NULL,
  `created_dt` datetime DEFAULT NULL,
  `last_update_dt` datetime DEFAULT NULL,
  `parameter_xml` text,
  PRIMARY KEY (`ecosystem_id`),
  UNIQUE KEY `name_cloud_account` (`account_id`,`ecosystem_name`),
  KEY `fk_cloud_account` (`account_id`),
  KEY `FK_ecotemplate_id` (`ecotemplate_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ecosystem_object` (
  `ecosystem_id` varchar(36) NOT NULL,
  `cloud_id` varchar(36) NOT NULL,
  `ecosystem_object_id` varchar(64) NOT NULL,
  `ecosystem_object_type` varchar(32) NOT NULL,
  `added_dt` datetime DEFAULT NULL,
  PRIMARY KEY (`ecosystem_id`,`ecosystem_object_id`,`ecosystem_object_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ecotemplate` (
  `ecotemplate_id` varchar(36) NOT NULL,
  `ecotemplate_name` varchar(64) DEFAULT NULL,
  `ecotemplate_desc` varchar(512) DEFAULT NULL,
  PRIMARY KEY (`ecotemplate_id`),
  UNIQUE KEY `ecotemplate_name` (`ecotemplate_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `ecotemplate_action` (
  `action_id` varchar(36) NOT NULL,
  `ecotemplate_id` varchar(36) NOT NULL,
  `action_name` varchar(64) NOT NULL,
  `action_desc` varchar(512) DEFAULT NULL,
  `category` varchar(32) DEFAULT NULL,
  `original_task_id` varchar(36) DEFAULT NULL,
  `task_version` decimal(18,3) DEFAULT NULL,
  `parameter_defaults` text,
  `action_icon` varchar(32) DEFAULT NULL,
  PRIMARY KEY (`action_id`),
  UNIQUE KEY `template_action` (`ecotemplate_id`,`action_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `global_registry` (
  `registry_id` varchar(36) NOT NULL,
  `registry_xml` text,
  PRIMARY KEY (`registry_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `import_task` (
  `user_id` varchar(36) NOT NULL DEFAULT '',
  `task_id` varchar(36) NOT NULL DEFAULT '',
  `original_task_id` varchar(36) NOT NULL DEFAULT '',
  `version` decimal(18,3) NOT NULL,
  `task_name` varchar(255) NOT NULL DEFAULT '',
  `task_code` varchar(32) DEFAULT NULL,
  `task_desc` varchar(255) DEFAULT '',
  `task_status` varchar(32) NOT NULL DEFAULT 'Development',
  `use_connector_system` int(11) NOT NULL DEFAULT '0',
  `default_version` int(11) NOT NULL,
  `concurrent_instances` int(11) DEFAULT NULL,
  `queue_depth` int(11) DEFAULT NULL,
  `created_dt` datetime NOT NULL,
  `parameter_xml` text NOT NULL,
  `import_mode` varchar(16) DEFAULT NULL,
  `conflict` varchar(255) DEFAULT NULL,
  `src_task_code` varchar(32) DEFAULT NULL,
  `src_task_name` varchar(255) DEFAULT NULL,
  `src_version` decimal(18,3) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
CREATE TABLE `import_task_codeblock` (
  `user_id` varchar(36) NOT NULL DEFAULT '',
  `task_id` varchar(36) NOT NULL DEFAULT '',
  `codeblock_name` varchar(32) NOT NULL DEFAULT ''
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
CREATE TABLE `import_task_step` (
  `user_id` varchar(36) NOT NULL DEFAULT '',
  `step_id` varchar(36) NOT NULL DEFAULT '',
  `task_id` varchar(36) NOT NULL DEFAULT '',
  `codeblock_name` varchar(36) NOT NULL DEFAULT '',
  `step_order` int(11) NOT NULL,
  `commented` int(11) NOT NULL DEFAULT '0',
  `locked` int(11) NOT NULL DEFAULT '0',
  `function_name` varchar(64) NOT NULL,
  `function_xml` text NOT NULL,
  `step_desc` varchar(255) DEFAULT '',
  `output_parse_type` int(11) NOT NULL,
  `output_row_delimiter` int(11) NOT NULL DEFAULT '0',
  `output_column_delimiter` int(11) NOT NULL DEFAULT '0',
  `variable_xml` text
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
CREATE TABLE `ldap_domain` (
  `ldap_domain` varchar(255) NOT NULL DEFAULT '',
  `address` varchar(255) NOT NULL DEFAULT '',
  PRIMARY KEY (`ldap_domain`(64))
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `login_security_settings` (
  `id` int(11) NOT NULL,
  `pass_max_age` int(11) NOT NULL,
  `pass_max_attempts` int(11) NOT NULL,
  `pass_max_length` int(11) NOT NULL,
  `pass_min_length` int(11) NOT NULL,
  `pass_complexity` int(11) NOT NULL,
  `pass_age_warn_days` int(11) NOT NULL,
  `pass_require_initial_change` int(11) NOT NULL,
  `auto_lock_reset` int(11) NOT NULL,
  `login_message` varchar(255) NOT NULL DEFAULT '',
  `auth_error_message` varchar(255) NOT NULL DEFAULT '',
  `pass_history` int(11) NOT NULL,
  `page_view_logging` int(11) NOT NULL,
  `report_view_logging` int(11) NOT NULL,
  `allow_login` int(11) NOT NULL,
  `new_user_email_message` varchar(1024) DEFAULT NULL,
  `log_days` int(11) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `logserver_settings` (
  `id` int(11) NOT NULL,
  `mode_off_on` varchar(3) NOT NULL DEFAULT '',
  `loop_delay_sec` int(11) NOT NULL,
  `port` int(11) NOT NULL,
  `log_file_days` int(11) NOT NULL,
  `log_table_days` int(11) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `lu_task_step_function_category` (
  `category_name` varchar(32) NOT NULL,
  `category_label` varchar(64) NOT NULL,
  `sort_order` decimal(18,2) NOT NULL,
  `description` varchar(255) DEFAULT '',
  `icon` varchar(255) DEFAULT '',
  PRIMARY KEY (`category_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `lu_task_step_function` (
  `function_name` varchar(64) NOT NULL,
  `function_label` varchar(64) NOT NULL,
  `category_name` varchar(32) NOT NULL,
  `sort_order` decimal(18,2) NOT NULL DEFAULT '0.00',
  `description` varchar(255) DEFAULT '',
  `help` text,
  `icon` varchar(255) DEFAULT '',
  `xml_template` text NOT NULL,
  PRIMARY KEY (`function_name`),
  KEY `FK_lu_task_step_function_lu_task_step_function_category` (`category_name`),
  CONSTRAINT `FK_lu_task_step_function_lu_task_step_function_category` FOREIGN KEY (`category_name`) REFERENCES `lu_task_step_function_category` (`category_name`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `message` (
  `msg_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `date_time_entered` datetime DEFAULT NULL,
  `date_time_completed` datetime DEFAULT NULL,
  `process_id` int(11) DEFAULT NULL,
  `process_type` int(11) DEFAULT NULL,
  `status` int(11) DEFAULT NULL,
  `error_message` varchar(255) DEFAULT '',
  `msg_to` varchar(255) DEFAULT '',
  `msg_from` varchar(255) DEFAULT '',
  `msg_subject` varchar(255) DEFAULT '',
  `msg_body` text,
  `retry` int(11) DEFAULT NULL,
  `num_retries` int(11) DEFAULT NULL,
  `msg_cc` varchar(255) DEFAULT '',
  `msg_bcc` varchar(255) DEFAULT '',
  PRIMARY KEY (`msg_id`),
  KEY `message_IX_message` (`status`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `message_data_file` (
  `msg_file_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `msg_id` bigint(20) DEFAULT NULL,
  `file_name` varchar(255) DEFAULT NULL,
  `file_type` int(11) DEFAULT NULL,
  `file_data` blob,
  PRIMARY KEY (`msg_file_id`),
  KEY `FK_message_data_file_message` (`msg_id`),
  CONSTRAINT `FK_message_data_file_message` FOREIGN KEY (`msg_id`) REFERENCES `message` (`msg_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `message_file_lookup` (
  `file_type` int(11) NOT NULL AUTO_INCREMENT,
  `file_description` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`file_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `messenger_settings` (
  `id` int(11) NOT NULL,
  `mode_off_on` varchar(3) NOT NULL,
  `loop_delay_sec` int(11) NOT NULL,
  `retry_delay_min` int(11) NOT NULL,
  `retry_max_attempts` int(11) NOT NULL,
  `smtp_server_addr` varchar(255) DEFAULT '',
  `smtp_server_user` varchar(255) DEFAULT '',
  `smtp_server_password` varchar(255) DEFAULT '1753-01-01 00:00:00',
  `smtp_server_port` int(11) DEFAULT NULL,
  `from_email` varchar(255) DEFAULT '',
  `from_name` varchar(255) DEFAULT '',
  `admin_email` varchar(255) DEFAULT '',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `object_registry` (
  `object_id` varchar(36) NOT NULL,
  `registry_xml` text NOT NULL,
  PRIMARY KEY (`object_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `object_tags` (
  `object_id` varchar(36) NOT NULL,
  `object_type` int(11) NOT NULL,
  `tag_name` varchar(64) NOT NULL,
  PRIMARY KEY (`object_id`,`tag_name`,`object_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `object_type` (
  `object_type` int(11) NOT NULL,
  `object_type_name` varchar(32) NOT NULL,
  `searchable` int(11) NOT NULL DEFAULT '0',
  `table_name` varchar(32) DEFAULT NULL,
  `id_col` varchar(32) DEFAULT NULL,
  `name_col` varchar(32) DEFAULT NULL,
  `item_selector_cols` varchar(255) DEFAULT '',
  `edit_page` varchar(64) DEFAULT NULL,
  `detail_report` varchar(36) DEFAULT NULL,
  `name_col_width` varchar(255) DEFAULT '',
  `selector_col_width` varchar(255) DEFAULT '',
  PRIMARY KEY (`object_type`),
  UNIQUE KEY `IX_object_type_name` (`object_type_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `poller_settings` (
  `id` int(11) NOT NULL,
  `mode_off_on` varchar(3) NOT NULL,
  `loop_delay_sec` int(11) NOT NULL,
  `max_processes` int(11) NOT NULL,
  `app_instance` varchar(1024) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `schedule` (
  `schedule_id` varchar(36) NOT NULL DEFAULT '',
  `schedule_name` varchar(255) NOT NULL DEFAULT '',
  `schedule_desc` varchar(255) DEFAULT '',
  `status` varchar(50) NOT NULL,
  `recurring` int(11) NOT NULL,
  `start_dt` datetime DEFAULT NULL,
  `stop_dt` datetime DEFAULT NULL,
  `months` varchar(27) DEFAULT NULL,
  `days_mon_wk_all` int(11) DEFAULT NULL,
  `days` varchar(84) DEFAULT NULL,
  `hours` varchar(62) DEFAULT NULL,
  `minutes` varchar(172) DEFAULT NULL,
  PRIMARY KEY (`schedule_id`),
  UNIQUE KEY `schedule_IX_sched_name` (`schedule_name`(64))
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `schedule_instance` (
  `schedule_instance` bigint(20) NOT NULL AUTO_INCREMENT,
  `schedule_instance_name` varchar(255) NOT NULL DEFAULT '',
  `schedule_id` varchar(36) NOT NULL DEFAULT '',
  `status` varchar(16) DEFAULT NULL,
  `run_dt` datetime DEFAULT NULL,
  `ran_dt` datetime DEFAULT NULL,
  PRIMARY KEY (`schedule_instance`),
  KEY `schedule_instance_IX_schedule_instance` (`status`,`run_dt`),
  KEY `FK_schedule_instance_schedule` (`schedule_id`),
  CONSTRAINT `FK_schedule_instance_schedule` FOREIGN KEY (`schedule_id`) REFERENCES `schedule` (`schedule_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `schedule_object` (
  `schedule_id` varchar(36) NOT NULL DEFAULT '',
  `object_id` varchar(36) NOT NULL DEFAULT '',
  `object_type` int(11) NOT NULL,
  `commented` int(11) NOT NULL,
  `ecosystem_id` varchar(36) DEFAULT NULL,
  PRIMARY KEY (`schedule_id`,`object_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `schedule_object_asset` (
  `schedule_id` varchar(36) NOT NULL DEFAULT '',
  `object_id` varchar(36) NOT NULL DEFAULT '',
  `asset_id` varchar(36) NOT NULL DEFAULT '',
  PRIMARY KEY (`schedule_id`,`object_id`,`asset_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `scheduler_settings` (
  `id` int(11) NOT NULL,
  `mode_off_on` varchar(3) NOT NULL,
  `loop_delay_sec` int(11) NOT NULL,
  `schedule_min_depth` int(11) NOT NULL,
  `schedule_max_days` int(11) NOT NULL,
  `clean_app_registry` int(11) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `task` (
  `task_id` varchar(36) NOT NULL DEFAULT '',
  `original_task_id` varchar(36) NOT NULL DEFAULT '',
  `version` decimal(18,3) NOT NULL,
  `task_name` varchar(255) NOT NULL DEFAULT '',
  `task_code` varchar(32) DEFAULT NULL,
  `task_desc` varchar(255) DEFAULT '',
  `task_status` varchar(32) NOT NULL DEFAULT 'Development',
  `use_connector_system` int(11) NOT NULL DEFAULT '0',
  `default_version` int(11) NOT NULL,
  `concurrent_instances` int(11) DEFAULT NULL,
  `queue_depth` int(11) DEFAULT NULL,
  `created_dt` datetime NOT NULL,
  `parameter_xml` text NOT NULL,
  PRIMARY KEY (`task_id`),
  UNIQUE KEY `IX_task_version` (`original_task_id`,`version`),
  UNIQUE KEY `IX_task_name_version` (`task_name`(64),`version`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `task_codeblock` (
  `task_id` varchar(36) NOT NULL DEFAULT '',
  `codeblock_name` varchar(32) NOT NULL DEFAULT '',
  PRIMARY KEY (`task_id`,`codeblock_name`),
  KEY `FK_task_codeblock_task` (`task_id`),
  CONSTRAINT `FK_task_codeblock_task` FOREIGN KEY (`task_id`) REFERENCES `task` (`task_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `task_conn_log` (
  `task_instance` bigint(20) NOT NULL,
  `address` varchar(128) NOT NULL,
  `userid` varchar(36) DEFAULT '',
  `conn_type` varchar(32) NOT NULL,
  `conn_dt` datetime NOT NULL,
  KEY `IX_task_conn_log_address` (`address`(64)),
  KEY `IX_task_conn_log_conn_dt` (`conn_dt`),
  KEY `IX_task_conn_log_ti` (`task_instance`),
  KEY `IX_task_conn_log_userid` (`userid`(32))
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `task_instance` (
  `task_instance` bigint(20) NOT NULL AUTO_INCREMENT,
  `task_id` varchar(36) NOT NULL DEFAULT '',
  `task_status` varchar(16) NOT NULL,
  `debug_level` int(11) NOT NULL DEFAULT '0',
  `asset_id` varchar(36) DEFAULT '',
  `submitted_by` varchar(36) DEFAULT '',
  `submitted_dt` datetime DEFAULT NULL,
  `started_dt` datetime DEFAULT NULL,
  `completed_dt` datetime DEFAULT NULL,
  `schedule_instance` bigint(20) DEFAULT NULL,
  `ce_node` int(11) DEFAULT NULL,
  `pid` int(11) DEFAULT NULL,
  `group_name` varchar(32) DEFAULT NULL,
  `submitted_by_instance` bigint(20) DEFAULT NULL,
  `ecosystem_id` varchar(36) DEFAULT NULL,
  `account_id` varchar(36) DEFAULT NULL,
  PRIMARY KEY (`task_instance`),
  KEY `IX_task_instance_asset_id` (`asset_id`),
  KEY `IX_task_instance_cenode` (`ce_node`),
  KEY `IX_task_instance_completed_dt` (`completed_dt`),
  KEY `IX_task_instance_started_dt` (`started_dt`),
  KEY `IX_task_instance_status_pid` (`task_status`,`pid`),
  KEY `IX_task_instance_task_id` (`task_id`),
  KEY `IX_task_instance_task_status` (`task_status`),
  KEY `IX_task_instance_schedule_instance` (`schedule_instance`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `task_instance_log` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `task_instance` bigint(20) NOT NULL,
  `step_id` varchar(36) DEFAULT '',
  `entered_dt` datetime DEFAULT NULL,
  `connection_name` varchar(36) DEFAULT NULL,
  `log` mediumtext,
  `command_text` text,
  PRIMARY KEY (`id`),
  KEY `task_instance_log_IX_task_instance_log` (`task_instance`,`entered_dt`),
  KEY `IX_task_instance_log_connection_name` (`connection_name`),
  CONSTRAINT `FK_task_instance_log_task_instance` FOREIGN KEY (`task_instance`) REFERENCES `task_instance` (`task_instance`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `task_instance_parameter` (
  `task_instance` bigint(20) NOT NULL,
  `parameter_xml` text NOT NULL,
  PRIMARY KEY (`task_instance`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `task_step` (
  `step_id` varchar(36) NOT NULL DEFAULT '',
  `task_id` varchar(36) NOT NULL DEFAULT '',
  `codeblock_name` varchar(36) NOT NULL DEFAULT '',
  `step_order` int(11) NOT NULL,
  `commented` int(11) NOT NULL DEFAULT '0',
  `locked` int(11) NOT NULL DEFAULT '0',
  `function_name` varchar(64) NOT NULL,
  `function_xml` text NOT NULL,
  `step_desc` varchar(255) DEFAULT '',
  `output_parse_type` int(11) NOT NULL,
  `output_row_delimiter` int(11) NOT NULL DEFAULT '0',
  `output_column_delimiter` int(11) NOT NULL DEFAULT '0',
  `variable_xml` text,
  PRIMARY KEY (`step_id`),
  KEY `task_step_IX_task_step` (`task_id`,`codeblock_name`,`commented`),
  KEY `IX_task_step_commented` (`commented`),
  KEY `IX_task_step_function_name` (`function_name`),
  CONSTRAINT `FK_task_step_task` FOREIGN KEY (`task_id`) REFERENCES `task` (`task_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `task_step_clipboard` (
  `user_id` varchar(36) NOT NULL DEFAULT '',
  `clip_dt` datetime NOT NULL,
  `src_step_id` varchar(36) NOT NULL DEFAULT '',
  `root_step_id` varchar(36) NOT NULL DEFAULT '',
  `step_id` varchar(36) NOT NULL DEFAULT '',
  `function_name` varchar(32) NOT NULL,
  `function_xml` text NOT NULL,
  `step_desc` varchar(255) DEFAULT '',
  `output_parse_type` int(11) NOT NULL,
  `output_row_delimiter` int(11) NOT NULL,
  `output_column_delimiter` int(11) NOT NULL,
  `variable_xml` text,
  `codeblock_name` varchar(36) DEFAULT NULL,
  PRIMARY KEY (`user_id`,`clip_dt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `task_step_user_settings` (
  `user_id` varchar(36) NOT NULL DEFAULT '',
  `step_id` varchar(36) NOT NULL DEFAULT '',
  `visible` int(11) NOT NULL,
  `breakpoint` int(11) NOT NULL,
  `skip` int(11) NOT NULL,
  `button` varchar(16) DEFAULT NULL,
  PRIMARY KEY (`user_id`,`step_id`),
  KEY `FK_task_step_user_settings_task_step` (`step_id`),
  KEY `FK_task_step_user_settings_users` (`user_id`),
  CONSTRAINT `FK_task_step_user_settings_task_step` FOREIGN KEY (`step_id`) REFERENCES `task_step` (`step_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_task_step_user_settings_users` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user_password_history` (
  `user_id` varchar(36) NOT NULL DEFAULT '',
  `change_time` datetime NOT NULL DEFAULT '1753-01-01 00:00:00',
  `password` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`user_id`,`change_time`),
  KEY `FK_user_password_history_users` (`user_id`),
  CONSTRAINT `FK_user_password_history_users` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user_security_log` (
  `log_id` bigint(20) NOT NULL AUTO_INCREMENT,
  `log_type` varchar(16) NOT NULL,
  `action` varchar(32) NOT NULL,
  `user_id` varchar(36) NOT NULL DEFAULT '',
  `log_dt` datetime NOT NULL,
  `object_type` int(11) DEFAULT NULL,
  `object_id` varchar(255) DEFAULT '',
  `log_msg` varchar(255) DEFAULT '',
  PRIMARY KEY (`log_id`),
  KEY `IX_user_security_log_log_dt` (`log_dt`),
  KEY `IX_user_security_log_log_type` (`log_type`),
  KEY `IX_user_security_log_object_id` (`object_id`(64)),
  KEY `IX_user_security_log_user_id` (`user_id`),
  KEY `FK_user_security_log_users` (`user_id`),
  CONSTRAINT `FK_user_security_log_users` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user_session` (
  `user_id` varchar(36) NOT NULL DEFAULT '',
  `address` varchar(255) NOT NULL DEFAULT '',
  `login_dt` datetime NOT NULL,
  `heartbeat` datetime NOT NULL,
  `kick` int(11) NOT NULL,
  PRIMARY KEY (`user_id`),
  KEY `FK_user_session_users` (`user_id`),
  CONSTRAINT `FK_user_session_users` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
/*!50001 CREATE TABLE `tv_application_registry` (
  `id` bigint(20),
  `app_name` varchar(255),
  `app_instance` varchar(255),
  `master` tinyint(1),
  `heartbeat` datetime,
  `last_processed_dt` datetime,
  `logfile_name` varchar(255),
  `load_value` decimal(18,3),
  `hostname` varchar(255),
  `userid` varchar(255),
  `pid` int(11),
  `executible_path` varchar(1024),
  `command_line` varchar(255),
  `platform` varchar(255)
) ENGINE=InnoDB */;
SET character_set_client = @saved_cs_client;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
/*!50001 CREATE TABLE `tv_schedule_instance` (
  `schedule_instance` bigint(20),
  `schedule_instance_name` varchar(255),
  `schedule_id` varchar(36),
  `status` varchar(16),
  `run_dt` datetime,
  `ran_dt` datetime
) ENGINE=InnoDB */;
SET character_set_client = @saved_cs_client;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
/*!50001 CREATE TABLE `tv_task_instance` (
  `task_instance` bigint(20),
  `task_id` varchar(36),
  `task_status` varchar(16),
  `debug_level` int(11),
  `asset_id` varchar(36),
  `submitted_by` varchar(36),
  `submitted_dt` datetime,
  `started_dt` datetime,
  `completed_dt` datetime,
  `schedule_instance` bigint(20),
  `ce_node` int(11),
  `pid` int(11),
  `group_name` varchar(32),
  `submitted_by_instance` bigint(20),
  `ecosystem_id` varchar(36),
  `account_id` varchar(36)
) ENGINE=InnoDB */;
SET character_set_client = @saved_cs_client;
/*!50001 DROP TABLE IF EXISTS `tv_application_registry`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8 */;
/*!50001 SET character_set_results     = utf8 */;
/*!50001 SET collation_connection      = utf8_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER= CURRENT_USER SQL SECURITY DEFINER */
/*!50001 VIEW `tv_application_registry` AS select `application_registry`.`id` AS `id`,`application_registry`.`app_name` AS `app_name`,`application_registry`.`app_instance` AS `app_instance`,`application_registry`.`master` AS `master`,`application_registry`.`heartbeat` AS `heartbeat`,`application_registry`.`last_processed_dt` AS `last_processed_dt`,`application_registry`.`logfile_name` AS `logfile_name`,`application_registry`.`load_value` AS `load_value`,`application_registry`.`hostname` AS `hostname`,`application_registry`.`userid` AS `userid`,`application_registry`.`pid` AS `pid`,`application_registry`.`executible_path` AS `executible_path`,`application_registry`.`command_line` AS `command_line`,`application_registry`.`platform` AS `platform` from `application_registry` */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!50001 DROP TABLE IF EXISTS `tv_schedule_instance`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8 */;
/*!50001 SET character_set_results     = utf8 */;
/*!50001 SET collation_connection      = utf8_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER= CURRENT_USER SQL SECURITY DEFINER */
/*!50001 VIEW `tv_schedule_instance` AS select `schedule_instance`.`schedule_instance` AS `schedule_instance`,`schedule_instance`.`schedule_instance_name` AS `schedule_instance_name`,`schedule_instance`.`schedule_id` AS `schedule_id`,`schedule_instance`.`status` AS `status`,`schedule_instance`.`run_dt` AS `run_dt`,`schedule_instance`.`ran_dt` AS `ran_dt` from `schedule_instance` */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!50001 DROP TABLE IF EXISTS `tv_task_instance`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8 */;
/*!50001 SET character_set_results     = utf8 */;
/*!50001 SET collation_connection      = utf8_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER= CURRENT_USER SQL SECURITY DEFINER */
/*!50001 VIEW `tv_task_instance` AS select `task_instance`.`task_instance` AS `task_instance`,`task_instance`.`task_id` AS `task_id`,`task_instance`.`task_status` AS `task_status`,`task_instance`.`debug_level` AS `debug_level`,`task_instance`.`asset_id` AS `asset_id`,`task_instance`.`submitted_by` AS `submitted_by`,`task_instance`.`submitted_dt` AS `submitted_dt`,`task_instance`.`started_dt` AS `started_dt`,`task_instance`.`completed_dt` AS `completed_dt`,`task_instance`.`schedule_instance` AS `schedule_instance`,`task_instance`.`ce_node` AS `ce_node`,`task_instance`.`pid` AS `pid`,`task_instance`.`group_name` AS `group_name`,`task_instance`.`submitted_by_instance` AS `submitted_by_instance`,`task_instance`.`ecosystem_id` AS `ecosystem_id`,`task_instance`.`account_id` AS `account_id` from `task_instance` */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8 */ ;
/*!50003 SET character_set_results = utf8 */ ;
/*!50003 SET collation_connection  = utf8_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = '' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50020 DEFINER= CURRENT_USER /*!50003 FUNCTION `FormatTextToHTML`(_str text) RETURNS text CHARSET latin1
BEGIN	/*DECLARE @strFixed text	SET @strFixed = @str*/	SET _str = replace(_str, '<', '&lt;');	SET _str = replace(_str, '>', '&gt;');	SET _str = replace(_str, char(13,10 USING utf8), '<br />');	/*SET _str = replace(_str, char(13,10 USING utf8)+char(10 USING utf8), '<br />');*/	SET _str = replace(_str, char(13 USING utf8), '<br />');	SET _str = replace(_str, char(10 USING utf8), '<br />');	SET _str = replace(_str, '     ', '&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;');	RETURN _str;END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8 */ ;
/*!50003 SET character_set_results = utf8 */ ;
/*!50003 SET collation_connection  = utf8_general_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'NO_BACKSLASH_ESCAPES' */ ;
DELIMITER ;;
/*!50003 CREATE*/ /*!50020 DEFINER= CURRENT_USER /*!50003 PROCEDURE `addTaskInstance`(
  IN _task_id varchar(36), 
  IN _user_id varchar(36), 
  IN _schedule_instance_id varchar(36), 
  IN _debug_level int, 
  IN _submitted_by_instance bigint(20), 
  IN _parameter_xml text, 
  IN _ecosystem_id varchar(36), 
  IN _account_id varchar(36)
)
BEGIN
    DECLARE _task_instance text;

		INSERT task_instance (
      task_status,
      submitted_dt,
      task_id,
      submitted_by,
      debug_level,
      schedule_instance,
      submitted_by_instance,
      ecosystem_id,
      account_id
    )
    VALUES (
      'Submitted',
      now(),
      _task_id,
      _user_id,
      _debug_level,
      _schedule_instance_id,
      _submitted_by_instance,
      _ecosystem_id,
      _account_id
    );

    SELECT LAST_INSERT_ID() INTO _task_instance;

    #parameters
if (_parameter_xml is not null) then 

      INSERT task_instance_parameter (
        task_instance,
        parameter_xml
      )
      VALUES (
        _task_instance,
        _parameter_xml
      );
end if;

    SELECT _task_instance;

 END */;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
