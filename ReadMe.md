Updates \*

As we discussed I did convert the existing Terraform template to Pulumi using the documentation and built-in API convert feature available at https://www.pulumi.com/tf2pulumi/.

Process: I created a new Pulumi environment and decided to use C#, as our services are already built in this language. However, if necessary, I am also able to redo the process in Node.js.

As a test run, I created a quick-example project using index.html and pulumi.aws.s3. Afterwards, I proceeded to convert the Terraform template step by step into Pulumi.

In my experience, Pulumi feels similar in functionality to AWS CDK.

Did access AWS credentials from the local AWS/config.

I have also included a few pictures as examples in the repository.

Please let me know if any further updates or redos are necessary.

---

After conducting a thorough review of the requirements and evaluating various options, I have determined that the best approach for this task would be to use ECS with Fargate.(out of ECS on ec2 or Beanstalk/EKS)
This decision was made based on several factors, including the need for high availability and the need for an efficient means of scaling(complicated to scale containers on ec2 + scaling the ec2s.). Additionally, I decided to deploy them loosely coupled to have a better option to scale. Not scale them together as a service.

I encountered an issue with the backend service being unable to pull images from ECR using a private subnet(https://stackoverflow.com/questions/61265108/aws-ecs-fargate-resourceinitializationerror-unable-to-pull-secrets-or-registry), and my options were to either launch tasks in a private subnet with a VPC routing table configured to route outbound traffic via a NAT gateway in a public subnet, or to launch tasks in a private subnet and ensure that the necessary AWS PrivateLink endpoints are configured. I decided to use the public addresses of my backend service to complete the task first and then add the security practices.

I have also added an ALB with target groups and listeners to redirect traffic from the ALB port 80 to the web UI port 5000. However, I am still facing issues with attaching a container to a target group and have not yet finished scaling for containers(based on cloudwatch metrics(most likely depending on cpu, not memory or throughput of alb)).

Additionally, there are a few more rules that need to be added to the security groups and I plan to move back from using a public IP to get the ECR image to using a NAT gateway in a public subnet.

- I do plan to update all the variable(+diagram) for vpc and subnets to be as a variables; and include .github/workflows/cd.yml -> to push images to ECR.

Containers do work if we pass a public ip as a apiAddress env variable: http://<public_ip>:5000//WeatherForecast.
