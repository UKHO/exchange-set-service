import http from 'k6/http';
import { check } from 'k6';

const apiClient = require('./clientHelper.js');
const dataHelper = require('./dataHelper.js');

const productIdentifierData = dataHelper.GetProductIdentifierData();

export default function (clientAuthResp) {
  let batchStatusUrl = apiClient.GetESSApiResponse("productIdentifiers", productIdentifierData, `${clientAuthResp.access_token}`);

  let fssCommitStatus = apiClient.GetFSSApiResponse(JSON.parse(batchStatusUrl));

  while (fssCommitStatus !== "Committed") {
    fssCommitStatus = apiClient.GetFSSApiResponse(JSON.parse(batchStatusUrl));
    if (fssCommitStatus == "Failed") {
      break;
    }
  };

  check(fssCommitStatus, {
    "status is Committed": fssCommitStatus == "Committed",
  })
}