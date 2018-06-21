def isDeploymentBranch(branch) {
	['master', 'jenkins-build'].contains(branch)
}

node {
	stage 'Checkout'
		deleteDir()
		checkout scm

	stage 'Build'
		withEnv(["PATH+NODE=${env.NODE8_PATH}"]) {
			bat 'build.cmd -t Package --production'
		}

	if(isDeploymentBranch(env.BRANCH_NAME)) {
		stage 'Deploy to pila IIS'
			withEnv(["DEPLOYMENT_PATH=\\\\pila.fp.lan\\inetpub\\wwwroot"]) {
				withCredentials(
					[usernamePassword(credentialsId: 'pila_deployment_user', 
						usernameVariable: 'DEPLOYMENT_USERNAME', 
						passwordVariable: 'DEPLOYMENT_PASSWORD')]) {
					bat 'build.cmd -t DeployArtifacts --production'
				}
			}	

		stage 'Deploy to pila SQL'
			withCredentials(
				[string(credentialsId: 'pila_deployment_connectionstring', 
					variable: 'DEPLOYMENT_CONNECTION_STRING')]) {
				bat 'build.cmd -t UpdateDB --production'
			}
	}

	stage 'Archive'
		archive 'artifacts/web/**'
}