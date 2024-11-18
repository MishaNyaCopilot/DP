redis-cli -h localhost -p 6000 shutdown &
redis-cli -h localhost -p 6001 shutdown &
redis-cli -h localhost -p 6002 shutdown &
sudo systemctl stop redis-server &
sudo systemctl stop nats-server
