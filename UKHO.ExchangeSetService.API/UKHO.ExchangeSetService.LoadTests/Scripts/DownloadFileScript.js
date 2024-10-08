import http from 'k6/http';
import { check, group } from 'k6';

const config = JSON.parse(open('../config.json'));
export function fileDownload(fssToken, fileDetails){
    var fileSize= JSON.stringify(fileDetails['files'][0]['fileSize']);
    var maxDownloadSize = new Number(config.DownloadFile.MaxDownloadBlockSizeInBytes);

    if(fileSize <= maxDownloadSize){                           
        fullFileDownload(fssToken, fileDetails);      
    }
    else {          
        partialFileDownload(fssToken,maxDownloadSize, fileSize, fileDetails);      
    }
}
export function fullFileDownload(fssToken, fileDetails){  
    let url =  `${config.FSS_URL}/batch/${fileDetails['batchId']}/files/${fileDetails['files'][0]['filename']}`; 

    let fssResponse = http.get(url, { headers: { Authorization: `Bearer ${fssToken}`, "Content-Type": "application/json" }, });        

    check(fssResponse, {'is file download status 200': (r) => r.status == 200});   
}
  
export function partialFileDownload(fssToken,maxDownloadSize,fileSize, fileDetails){
    for(let i=0; i<fileDetails['files'].length; i++)
    {
        let startByte = 0;
        let downloadSize = maxDownloadSize;
        let endByte = downloadSize;
        let counter = 1;
        let fssResponse;
        fileSize= JSON.stringify(fileDetails['files'][i]['fileSize']);
        group('Partial File Download', () => {
            while (startByte <= endByte)
            {     
                var rangeHeader = "bytes="+ startByte +"-"+ endByte;        
                let url =  `${config.FSS_URL}/batch/${fileDetails['batchId']}/files/${fileDetails['files'][i]['filename']}`;              
                
                fssResponse = http.get(url, { headers: { Authorization: `Bearer ${fssToken}`, "Content-Type": "application/json", "Range": rangeHeader }});

                check(fssResponse, { 'is file download status 206': (r) => r.status == 206});
                let fssStatus = JSON.stringify(JSON.parse(fssResponse.status));

                if (fssStatus != 206) {
                    console.log(fileDetails['batchId'] + " : " + fssStatus);
                    console.log(fileDetails['batchId'] + " : " + startByte + " : " + endByte);
                }
                else {
                    startByte = endByte + 1;
                    endByte = endByte + downloadSize;
                }

                if(endByte > fileSize - 1)
                {
                    endByte = fileSize - 1;
                }   
                counter++;
            }
        });

    check(endByte, {'is complete file downloaded': endByte === fileSize-1});
    }

}