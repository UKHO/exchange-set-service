import { group, sleep } from 'k6';

const apiClient = require('./clientHelper.js');

export function ESSCreation(clientAuthResp, productVersionData, exchangeSetType) {
    group('ESS Creation', () => {
        let batchStatusUrl = apiClient.GetESSApiResponse("productVersions", productVersionData, `${clientAuthResp.essToken}`, exchangeSetType);
        sleep(1);
        console.log("batchStatusUrl", batchStatusUrl);
    });
}