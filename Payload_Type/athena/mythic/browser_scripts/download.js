function(task, responses){
  if(task.completed === true && task.status !== 'error'){
    try{
        let status = JSON.parse(responses[0]['response']);
        if(status.hasOwnProperty('agent_file_id')){
        	let file_name = status['filename'];
			  output = "<div class='card'><div class='card-header border border-dark shadow'>Finished Downloading <span class='display'>" + escapeHTML(file_name) + "</span>. Click <a href='/api/v1.4/files/download/" + status['agent_file_id'] + "'>here</a> to download</div></div>";
		      return { "plaintext" : output }
	    	}
    }catch(error){
		output = "<pre>Error: " + error.toString() + "\n" + escapeHTML(JSON.stringify(responses, null, 2)) + "</pre>";
		return { "plaintext" : output }
    }
  }
  if(task.status === 'error'){
	  return { "plaintext" : "<pre> Error: untoggle for error message(s) </pre>" };
  }
  return { "plaintext" : "<pre> Downloading... </pre>" };
}