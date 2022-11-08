# Update
FoxIDs is updated by updating the two test slots of the App Services FoxIDs and FoxIDs Control. Updates is picked up from the [master branch](https://github.com/ITfoxtec/FoxIDs) in GitHub with Kudu.

1. Open the [Azure portal](https://portal.azure.com/) and navigate to the FoxIDs resource group.
2. First navigate to the FoxIDs App Services test slots `foxidsxxxxxxxx/test`. 
3. Click Deployment Center and click Sync (this initiate the deployment sequence). You can follow the status on the Logs tab.
4. Then wait for it to update and automatically promoted the new version from the test slots to the production slots. 
2. Then navigate to the FoxIDs Control App Services test slots `foxidscontrolxxxxxxxx/test`. 
3. Click Deployment Center and click Sync. You can follow the status on the Logs tab.
4. Then wait for it to update and automatically promoted the new version from the test slots to the production slots. 
5. That is it you are done. You can see the exact version number by hovering the version number at the bottom of the page of both the FoxIDs and FoxIDs Control sites.

> It is possible to change the automatically promoted from the test slots to the production slots to manually initiated.
