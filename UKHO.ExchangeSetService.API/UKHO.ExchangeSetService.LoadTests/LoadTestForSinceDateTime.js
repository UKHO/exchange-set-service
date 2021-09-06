import http from 'k6/http';
import { group, sleep } from 'k6';

const apiClient = require('./clientHelper.js');

export function ESSCreation(clientAuthResp, sinceDateTime, exchangeSetType) {

    group('ESS Creation', () => {

        let batchStatusUrl = apiClient.GetESSApiResponseForSinceDateTime(sinceDateTime, `${clientAuthResp.essToken}`, exchangeSetType);
        sleep(1);
        console.log("batchStatusUrl", batchStatusUrl);
    });
}