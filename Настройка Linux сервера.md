##                                                    ##
# Настройка сервера #
##                                                    ##

> sudo apt update && sudo apt upgrade -y
> sudo apt install redis -y
> wget https://github.com/nats-io/nats-server/releases/download/v2.10.18/nats-server-v2.10.18-linux-amd64.tar.gz
> tar -zxf nats-server-*.tar.gz
> sudo cp nats-server-*-linux-amd64/nats-server /usr/bin/
> rm -rf nats-server-*-linux-amd64 nats-server-*.tar.gz
> sudo mkdir /etc/nats
> sudo nano /etc/nats/nats-server.conf

##                                        ##
# Конфиг для nats сервера #
##                                        ##
--------------------------------------------
cluster {
  name: "test-nats"
}

store_dir: "/var/lib/nats"
listen: "0.0.0.0:4222"
log_file: /var/log/nats/nats.log

--------------------------------------------

> sudo useradd -r -c 'NATS service' nats
> sudo mkdir /var/log/nats /var/lib/nats
> sudo chown nats:nats /var/log/nats /var/lib/nats
> sudo nano /etc/systemd/system/nats-server.service

##                                        ##
# Конфиг для сервиса nats сервера #
##                                        ##
--------------------------------------------
[Unit]
Description=NATS messaging server
After=syslog.target network.target

[Service]
Type=simple
ExecStart=/usr/bin/nats-server -c /etc/nats/nats-server.conf
User=nats
Group=nats
LimitNOFILE=65536
ExecReload=/bin/kill -HUP $MAINPID
Restart=on-failure

[Install]
WantedBy=multi-user.target
--------------------------------------------

> sudo systemctl enable nats-server --now
> sudo ufw allow 4222
> sudo ufw allow 6379