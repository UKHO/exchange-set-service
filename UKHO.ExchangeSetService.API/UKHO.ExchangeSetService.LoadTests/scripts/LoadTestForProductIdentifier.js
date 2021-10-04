import http from 'k6/http';
import { check, group, sleep } from 'k6';

const apiClient = require('./helper/clientHelper.js');

export function ESSCreation(clientAuthResp, productIdentifierData, exchangeSetType) {
    let fssCommitStatus;
    group('ESS Creation', () => {
        let batchStatusUrl = apiClient.GetESSApiResponse("productIdentifiers", productIdentifierData, `${clientAuthResp.essToken}`, exchangeSetType);
        sleep(1);
       
        fssCommitStatus = apiClient.GetFSSApiResponse(JSON.parse(batchStatusUrl), `${clientAuthResp.fssToken}`);

        while (fssCommitStatus !== "Committed") {
            sleep(10);
            fssCommitStatus = apiClient.GetFSSApiResponse(JSON.parse(batchStatusUrl), `${clientAuthResp.fssToken}`);

            if (fssCommitStatus === "Failed") {
                break;
            }
        };
    });
    
    check(fssCommitStatus, {
        "status is Committed": fssCommitStatus === "Committed",
    })
    console.log("batchStatusUrl", batchStatusUrl);
    console.log("fssCommitStatus", fssCommitStatus, exchangeSetType);
}