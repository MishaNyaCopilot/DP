##                           ##
# Проверка состояния серверов #
##                           ##

> sudo systemctl status redis-server
> sudo systemctl status nats-server

##                             ##
# Включение/Отключение серверов #
##                             ##

> sudo systemctl start redis-server
> sudo systemctl start nats-server
-----------------------------------
> sudo systemctl stop redis-server
> sudo systemctl stop nats-server

##                                           ##
# Запуск/Остановка redis-server с параметрами #
##                                           ##

> redis-server --port "номер порта"
---------------------------------------------
> redis-cli -p "номер порта" shutdown
> redis-cli -h "ip-адрес" -p "номер порта" shutdown