import http from 'k6/http';
import { check, setTimeout } from 'k6';

const apiClient = require('./clientHelper.js');
const dataHelper = require('./dataHelper.js');

const productVersionsData =  dataHelper.GetProductVersionData(); 

export default function (clientAuthResp) {
    let batchStatusUrl = apiClient.GetESSApiResponse("productVersions", productVersionsData,`${clientAuthResp.access_token}`);
   
    let fssCommitStatus =  apiClient.GetFSSApiResponse(JSON.parse(batchStatusUrl));
   
      while(fssCommitStatus != 'Committed'){
       setTimeout(() => {
        let fssCommitStatus =  apiClient.GetFSSApiResponse(JSON.parse(batchStatusUrl));
       }, "3000");
      
       if(fssCommitStatus === 'Failed'){
         break;
       }
     }

    check(fssCommitStatus, {
      'status is Committed': fssCommitStatus == 'Committed',
    })
}