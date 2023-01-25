using Pulumi;
using Aws = Pulumi.Aws;

class MyStack : Stack
{
    public MyStack()
    {
        var mainVpc = new Aws.Ec2.Vpc("mainVpc", new Aws.Ec2.VpcArgs
        {
            CidrBlock = "10.0.0.0/16",
            Tags = 
            {
                { "Owner", "Anatoliy Serputov" },
            },
        });
        
        var public1 = new Aws.Ec2.Subnet("public1", new Aws.Ec2.SubnetArgs
        {
            VpcId = mainVpc.Id,
            CidrBlock = "10.0.1.0/24",
            AvailabilityZone = "us-east-1a",
            MapPublicIpOnLaunch = true,
            Tags = 
            {
                { "Name", "public-subnet-1" },
            },
        });
        
        var public2 = new Aws.Ec2.Subnet("public2", new Aws.Ec2.SubnetArgs
        {
            VpcId = mainVpc.Id,
            CidrBlock = "10.0.4.0/24",
            AvailabilityZone = "us-east-1b",
            MapPublicIpOnLaunch = true,
            Tags = 
            {
                { "Name", "public-subnet-2" },
            },
        });
        
        var public_alb1 = new Aws.Ec2.Subnet("public-alb1", new Aws.Ec2.SubnetArgs
        {
            VpcId = mainVpc.Id,
            CidrBlock = "10.0.5.0/24",
            MapPublicIpOnLaunch = true,
            Tags = 
            {
                { "Name", "public-alb1" },
            },
        });
        
        var public_alb2 = new Aws.Ec2.Subnet("public-alb2", new Aws.Ec2.SubnetArgs
        {
            VpcId = mainVpc.Id,
            CidrBlock = "10.0.6.0/24",
            MapPublicIpOnLaunch = true,
            Tags = 
            {
                { "Name", "public-alb2" },
            },
        });
        
        var private1 = new Aws.Ec2.Subnet("private1", new Aws.Ec2.SubnetArgs
        {
            VpcId = mainVpc.Id,
            CidrBlock = "10.0.2.0/24",
            AvailabilityZone = "us-east-1a",
            MapPublicIpOnLaunch = false,
            Tags = 
            {
                { "Name", "private-subnet-1" },
            },
        });
        
        var private2 = new Aws.Ec2.Subnet("private2", new Aws.Ec2.SubnetArgs
        {
            VpcId = mainVpc.Id,
            CidrBlock = "10.0.3.0/24",
            AvailabilityZone = "us-east-1a",
            MapPublicIpOnLaunch = false,
            Tags = 
            {
                { "Name", "private-subnet-2" },
            },
        });
        
        var mainInternetGateway = new Aws.Ec2.InternetGateway("mainInternetGateway", new Aws.Ec2.InternetGatewayArgs
        {
            VpcId = mainVpc.Id,
            Tags = 
            {
                { "Name", "internet-gateway" },
            },
        });
        
        var mainRouteTable = new Aws.Ec2.RouteTable("mainRouteTable", new Aws.Ec2.RouteTableArgs
        {
            VpcId = mainVpc.Id,
        });
        
        var internetAccess = new Aws.Ec2.Route("internetAccess", new Aws.Ec2.RouteArgs
        {
            RouteTableId = mainRouteTable.Id,
            DestinationCidrBlock = "0.0.0.0/0",
            GatewayId = mainInternetGateway.Id,
        });
        
        var public1Subnet = new Aws.Ec2.RouteTableAssociation("public1Subnet", new Aws.Ec2.RouteTableAssociationArgs
        {
            SubnetId = public1.Id,
            RouteTableId = mainRouteTable.Id,
        });

        var public2Subnet = new Aws.Ec2.RouteTableAssociation("public2Subnet", new Aws.Ec2.RouteTableAssociationArgs
        {
            SubnetId = public2.Id,
            RouteTableId = mainRouteTable.Id,
        });

        var ecsTaskExecutionRole = new Aws.Iam.Role("ecsTaskExecutionRole", new Aws.Iam.RoleArgs
        {
            AssumeRolePolicy = @"{
               ""Version"": ""2012-10-17"",
               ""Statement"": [
                  {
                     ""Effect"": ""Allow"",
                     ""Principal"": {
                        ""Service"": ""ecs-tasks.amazonaws.com""
                        },
                        ""Action"": ""sts:AssumeRole"" 
                  }
               ]
            }",
        });

        var ecsTaskExecutionPolicy = new Aws.Iam.RolePolicy("ecsTaskExecutionPolicy", new Aws.Iam.RolePolicyArgs
        {
            Role = ecsTaskExecutionRole.Id,
            Policy = @"{
               ""Version"": ""2012-10-17"",
               ""Statement"": [
                  {
                     ""Effect"": ""Allow"",
                     ""Action"": [
                        ""ecr:GetAuthorizationToken"",
                        ""ecr:BatchCheckLayerAvailability"",
                        ""ecr:GetDownloadUrlForLayer"",
                        ""ecr:BatchGetImage""
                        ],
                        ""Resource"": ""*""
                  }
               ]
            }",
        });

        var infraApi = new Aws.Ecs.TaskDefinition("infraApi", new Aws.Ecs.TaskDefinitionArgs
        {
            Family = "infra-api",
            RequiresCompatibilities = 
            {
                "FARGATE",
            },
            NetworkMode = "awsvpc",
            ExecutionRoleArn = "arn:aws:iam::552641315216:role/ecsTaskExecutionRole",
            TaskRoleArn = "arn:aws:iam::552641315216:role/ecsTaskExecutionRole",
            Cpu = "256",
            Memory = "512",
            ContainerDefinitions = @"[
               {
                  ""name"": ""infra-api"",
                  ""image"": ""552641315216.dkr.ecr.us-east-1.amazonaws.com/infra-api:latest"",
                  ""portMappings"": [
                     {
                        ""containerPort"": 5000,
                        ""hostPort"": 5000
                     }
                  ],
               ""essential"": true
               }
            ]",
        });

        var sg = new Aws.Ec2.SecurityGroup("mySecurityGroup", new Aws.Ec2.SecurityGroupArgs
        {
            Description = "Allow incoming traffic on HTTP port 5000",
            VpcId = mainVpc.Id,
            Ingress = 
            {
                new Aws.Ec2.Inputs.SecurityGroupIngressArgs
                {
                    FromPort = 5000,
                    ToPort = 5000,
                    Protocol = "tcp",
                    CidrBlocks = 
                    {
                        "0.0.0.0/0",
                    },
                },
            },
            Egress = 
            {
                new Aws.Ec2.Inputs.SecurityGroupEgressArgs
                {
                    FromPort = 0,
                    ToPort = 0,
                    Protocol = "-1",
                    CidrBlocks = 
                    {
                        "0.0.0.0/0",
                    },
                },
            },
        });

        var app1 = new Aws.Ecs.Cluster("app1", new Aws.Ecs.ClusterArgs
        {
        });

        var infraApiTask = new Aws.Ecs.Service("infraApiTask", new Aws.Ecs.ServiceArgs
        {
            TaskDefinition = infraApi.Arn,
            Cluster = app1.Arn,
            DesiredCount = 1,
            LaunchType = "FARGATE",
            NetworkConfiguration = new Aws.Ecs.Inputs.ServiceNetworkConfigurationArgs
            {
                AssignPublicIp = true,
                Subnets = 
                {
                    public1.Id,
                },
                SecurityGroups = 
                {
                    sg.Id,
                },
            },
        });

        var infraWeb = new Aws.Ecs.TaskDefinition("infraWeb", new Aws.Ecs.TaskDefinitionArgs
        {
            Family = "infra-web",
            RequiresCompatibilities = 
            {
                "FARGATE",
            },
            NetworkMode = "awsvpc",
            ExecutionRoleArn = "arn:aws:iam::552641315216:role/ecsTaskExecutionRole",
            TaskRoleArn = "arn:aws:iam::552641315216:role/ecsTaskExecutionRole",
            Cpu = "256",
            Memory = "512",
            ContainerDefinitions = @"[
               {
                  ""name"": ""infra"",
                  ""image"": ""552641315216.dkr.ecr.us-east-1.amazonaws.com/infra:latest"",
                  ""portMappings"": [
                     {
                        ""containerPort"": 5000,
                        ""hostPort"": 5000
                        }
                        ],
                        ""essential"": true,
                        ""environment"": [
                           {
                              ""name"": ""ApiAddress"",
                              ""value"": ""http://1.0:5000/WeatherForecast""
                           }
                        ]
                     }
                  ]",
        });

        var infraWebTask = new Aws.Ecs.Service("infraWebTask", new Aws.Ecs.ServiceArgs
        {
            TaskDefinition = infraWeb.Arn,
            Cluster = app1.Arn,
            DesiredCount = 1,
            LaunchType = "FARGATE",
            NetworkConfiguration = new Aws.Ecs.Inputs.ServiceNetworkConfigurationArgs
            {
                AssignPublicIp = true,
                Subnets = 
                {
                    public2.Id,
                },
                SecurityGroups = 
                {
                    sg.Id,
                },
            },
        });

        var mainLoadBalancer = new Aws.Alb.LoadBalancer("mainLoadBalancer", new Aws.Alb.LoadBalancerArgs
        {
            Internal = false,
            SecurityGroups = 
            {
                sg.Id,
            },
            Subnets = 
            {
                public_alb1.Id,
                public_alb2.Id,
            },
        });

        var api = new Aws.Alb.TargetGroup("api", new Aws.Alb.TargetGroupArgs
        {
            Port = 5000,
            Protocol = "HTTP",
            VpcId = mainVpc.Id,
        });
        
        var uiTargetGroup = new Aws.Alb.TargetGroup("uiTargetGroup", new Aws.Alb.TargetGroupArgs
        {
            Port = 5000,
            Protocol = "HTTP",
            VpcId = mainVpc.Id,
        });

        var http_ui = new Aws.Alb.Listener("http-ui", new Aws.Alb.ListenerArgs
        {
            LoadBalancerArn = mainLoadBalancer.Arn,
            Port = 80,
            Protocol = "HTTP",
            DefaultActions = 
            {
                new Aws.Alb.Inputs.ListenerDefaultActionArgs
                {
                    TargetGroupArn = uiTargetGroup.Arn,
                    Type = "forward",
                },
            },
        });

        var http_api = new Aws.Alb.Listener("http-api", new Aws.Alb.ListenerArgs
        {
            LoadBalancerArn = mainLoadBalancer.Arn,
            Port = 5000,
            Protocol = "HTTP",
            DefaultActions = 
            {
                new Aws.Alb.Inputs.ListenerDefaultActionArgs
                {
                    TargetGroupArn = api.Arn,
                    Type = "forward",
                },
            },
        });
      //   var uiTargetGroupAttachment = new Aws.LB.TargetGroupAttachment("uiTargetGroupAttachment", new Aws.LB.TargetGroupAttachmentArgs
      //   {
      //       TargetGroupArn = uiTargetGroup.Arn,
      //       Port = 5000,
      //   });
    }

}


class Program
{
    static void Main(string[] args)
    {
        Deployment.RunAsync<MyStack>().Wait();
    }
}