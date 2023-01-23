provider "aws" {
  region = "us-east-1"
}

resource "aws_vpc" "main" {
  cidr_block = "10.0.0.0/16"
  tags = {
    Owner = "Anatoliy Serputov"
  }
}

resource "aws_subnet" "public1" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.1.0/24"
  availability_zone = "us-east-1a"
  map_public_ip_on_launch = true
  tags = {
    Name = "public-subnet-1"
  }
}

resource "aws_subnet" "public2" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.4.0/24"
  availability_zone = "us-east-1b"
  map_public_ip_on_launch = true
  tags = {
    Name = "public-subnet-2"
  }
}

resource "aws_subnet" "public-alb" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.5.0/24"
  map_public_ip_on_launch = true
  tags = {
    Name = "public-alb"
  }
}

resource "aws_subnet" "private1" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.2.0/24"
  availability_zone = "us-east-1a"
  map_public_ip_on_launch = false
  tags = {
    Name = "private-subnet-1"
  }
}

resource "aws_subnet" "private2" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.3.0/24"
  availability_zone = "us-east-1a"
  map_public_ip_on_launch = false
  tags = {
    Name = "private-subnet-2"
  }
}

resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id
  tags = {
    Name = "internet-gateway"
  }
}

resource "aws_route_table" "main" {
  vpc_id = aws_vpc.main.id
}

resource "aws_route" "internet_access" {
  route_table_id = aws_route_table.main.id
  destination_cidr_block = "0.0.0.0/0"
  gateway_id = aws_internet_gateway.main.id
}

resource "aws_route_table_association" "public1_subnet" {
  subnet_id = aws_subnet.public1.id
  route_table_id = aws_route_table.main.id
}

resource "aws_route_table_association" "public2_subnet" {
  subnet_id = aws_subnet.public2.id
  route_table_id = aws_route_table.main.id
}

resource "aws_iam_role" "ecs_task_execution_role" {
  name = "ecs_task_execution_role"
  assume_role_policy = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "ecs-tasks.amazonaws.com"
      },
      "Action": "sts:AssumeRole"
    }
  ]
}
EOF
}

resource "aws_iam_role_policy" "ecs_task_execution_policy" {
  name = "ecs_task_execution_policy"
  role = aws_iam_role.ecs_task_execution_role.id
  policy = <<EOF
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ecr:GetAuthorizationToken",
                "ecr:BatchCheckLayerAvailability",
                "ecr:GetDownloadUrlForLayer",
                "ecr:BatchGetImage"
            ],
            "Resource": "*"
        }
    ]
}
EOF
}

resource "aws_ecs_task_definition" "infra_api" {
  family = "infra-api"
  requires_compatibilities = ["FARGATE"]
  network_mode = "awsvpc"
  execution_role_arn = "arn:aws:iam::552641315216:role/ecsTaskExecutionRole"
  task_role_arn = "arn:aws:iam::552641315216:role/ecsTaskExecutionRole"
  cpu = "256"
  memory = "512"
  container_definitions = <<EOF
[
  {
    "name": "infra-api",
    "image": "552641315216.dkr.ecr.us-east-1.amazonaws.com/infra-api:latest",
    "portMappings": [
      {
        "containerPort": 5000,
        "hostPort": 5000
      }
    ],
    "essential": true
  }
]
EOF
}

resource "aws_security_group" "sg" {
  description = "Allow incoming traffic on HTTP port 5000"
  vpc_id = aws_vpc.main.id
  ingress {
    from_port   = 5000
    to_port     = 5000
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_ecs_cluster" "app1" {
  name = "app1"
}

resource "aws_ecs_service" "infra_api_task" {
  name = "infra-api-service"
  task_definition = aws_ecs_task_definition.infra_api.arn
  cluster = aws_ecs_cluster.app1.arn
  desired_count = 1
  launch_type = "FARGATE"
  network_configuration {
    assign_public_ip = true
    subnets = [aws_subnet.public1.id]
    security_groups = [aws_security_group.sg.id]
  }
}

resource "aws_ecs_task_definition" "infra_web" {
  family = "infra-web"
  requires_compatibilities = ["FARGATE"]
  network_mode = "awsvpc"
  execution_role_arn = "arn:aws:iam::552641315216:role/ecsTaskExecutionRole"
  task_role_arn = "arn:aws:iam::552641315216:role/ecsTaskExecutionRole"
  cpu = "256"
  memory = "512"
  container_definitions = <<EOF
[
  {
    "name": "infra",
    "image": "552641315216.dkr.ecr.us-east-1.amazonaws.com/infra:latest",
    "portMappings": [
      {
        "containerPort": 5000,
        "hostPort": 5000
      }
    ],
    "essential": true,
     "environment": [
        {
            "name": "ApiAddress",
            "value": "http://1.0:5000/WeatherForecast"
        }
    ]
  }
]
EOF
}

resource "aws_ecs_service" "infra_web_task" {
  name = "infra-web-service"
  task_definition = aws_ecs_task_definition.infra_web.arn
  cluster = aws_ecs_cluster.app1.arn
  desired_count = 1
  launch_type = "FARGATE" 
  network_configuration {
    assign_public_ip = true
    subnets = [aws_subnet.public2.id]
    security_groups = [aws_security_group.sg.id]
  }
}

resource "aws_alb" "main" {
  name            = "main"
  internal        = false
  security_groups = [aws_security_group.sg.id]
  subnets         = [aws_subnet.public-alb.id]
}

resource "aws_alb_target_group" "api" {
  name = "container1-tg"
  port = 5000
  protocol = "HTTP"
  vpc_id = aws_vpc.main.id
}

resource "aws_alb_target_group" "ui" {
  name = "container2-tg"
  port = 5000
  protocol = "HTTP"
  vpc_id = aws_vpc.main.id
}

resource "aws_alb_listener" "http-ui" {
  load_balancer_arn = aws_alb.main.arn
  port = "80"
  protocol = "HTTP"

  default_action {
    target_group_arn = aws_alb_target_group.ui.arn
    type = "forward"
  }
}

resource "aws_alb_listener" "http-api" {
  load_balancer_arn = aws_alb.main.arn
  port = "5000"
  protocol = "HTTP"

  default_action {
    target_group_arn = aws_alb_target_group.api.arn
    type = "forward"
  }
}

resource "aws_lb_target_group_attachment" "ui" {
  target_group_arn = aws_alb_target_group.ui.arn
  // target_id = aws_ecs_service.infra_web_task.arn
  port = 5000
}