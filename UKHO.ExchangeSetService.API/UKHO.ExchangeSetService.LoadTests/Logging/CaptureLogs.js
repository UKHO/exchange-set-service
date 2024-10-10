export function essConsoleLog(essResponse){
    if ( essResponse.status === 200)
    {
        let jsonResponse = JSON.parse(essResponse.body);
        let batchStatusUrl = JSON.stringify(jsonResponse['_links']['exchangeSetBatchStatusUri']['href']);
    
        console.log("batchStatusUrl:" + batchStatusUrl + " Status:" + essResponse.status);
    }
    else
    {
        console.log(" Status:" + essResponse.status); 
    }
}