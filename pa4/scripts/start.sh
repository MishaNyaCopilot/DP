sudo systemctl start nats-server &
sudo systemctl start redis-server &
redis-server --port 6000 &
redis-server --port 6001 &
redis-server --port 6002 &
