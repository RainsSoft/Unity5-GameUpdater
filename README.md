##这是GameUpdater的扩展程序

GameUpdater是Unity3D的一个非常好用的热更新插件.

他的思路是每次登陆游戏检查版本,如果和远程版本不同,则对比版本文件里需要更新的文件,然后逐个从远程服务器上更新bundle文件.

第一次Build Bundle到Resources路径下,然后每一次Build Bundle到AssetBundlePool路径下,Build的Bundle都是经过7Zip压缩和加密,所以Bundle文件会特别小,每次把版本信息记录在一个版本文件里.每次LoadBundle时解密解压缩.

##优势:

如果有10个版本的更新,则可以从当前版本直接更新到最新版本,而不必考虑各个版本的差异

##劣势:
如果版本更新文件较多则与远程服务器IO操作较多.




##Note:
