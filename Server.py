#coding:utf-8
from socket import *
import struct
import sqlite3
import random
import sys

import heapq


#三维坐标
class Vector3:
    def __init__(self, x = 0, y = 0, z = 0):
        self.x = x
        self.y = y
        self.z = z

    def readFromFile(self, fileReader):
        data = fileReader.read(12)
        if not data:
            return False
        self.x = struct.unpack_from('i', data, 0)[0]
        self.y = struct.unpack_from('i', data, 4)[0]
        self.z = struct.unpack_from('i', data, 8)[0]
        print self.x, self.y, self.z
        return True

    @staticmethod
    def readNewFromFile(fileReader):
        "若读到文件末尾返回空字符"
        data = fileReader.read(12)
        if not data:
            return (data, fileReader)
        v3 = Vector3()
        v3.x = struct.unpack_from('i', data, 0)[0]
        v3.y = struct.unpack_from('i', data, 4)[0]
        v3.z = struct.unpack_from('i', data, 8)[0]
        print v3.x, v3.y, v3.z, fileReader.tell()
        return (v3, fileReader)


#寻路相关
def getDistance(x1, z1, x2, z2):
    return abs(x1 - x2) + abs(z1 - z2)

class NodeInfo:
    def __init__(self, position, parent, cost):
        self.position = position
        self.parent = parent
        self.cost = cost

    def __lt__(self, other):
        return self.cost < other.cost

def searchRoad(startX, startZ, endX, endZ, layoutDict):
    "传入参数应为整数"
    openNode = []   #按代价排列的最小堆
    openDict = {}    #方便查询是否在open节点中，对应的值为NodeInfo对象
    closeDict = {(startX, 0, startZ): NodeInfo((startX, 0, startZ), None, 0)}  #方便查询是否在close节点中
    nowX = startX
    nowZ = startZ
    rangeListX = [0, -1, 1]
    rangeListY = [-1, 1, 0]
    rangeList = [(0, -1), (0, 1), (-1, 0), (1, 0), (1, -1), (1, 1), (-1, 1), (-1, -1)]

    isOver = False

    while True:
        # print nowX, nowZ
        # for i in rangeListX:
        #     for j in rangeListY:
        for pos in rangeList:
            # if i == 0 and j == 0:
            #     continue
            i = pos[0]
            j = pos[1]

            x = nowX + i
            z = nowZ + j
            if x == endX and z == endZ: #若搜寻到终点算法结束，此时终点的parent节点为(nowX, 0, nowZ)
                isOver = True
                break

            if layoutDict.has_key((x, 0, z)) and layoutDict[(x, 0, z)] != 0:    #若为可到达位置
                if not closeDict.has_key((x, 0, z)):    #若不在close中
                    cost = getDistance(startX, startZ, x, z) + getDistance(x, z, endX, endZ)    #计算代价
                    if openDict.has_key((x, 0, z)): #若已经在open节点中则更新代价
                        ind = openNode.index(openDict[(x, 0, z)])
                        if openNode[ind].cost > cost:
                            print 'Heapify'
                            openNode[ind].cost = cost
                            openNode[ind].parent = (nowX, 0, nowZ)
                            heapq.heapify(openNode)
                    else:   #否则加入open中
                        nodeInfo = NodeInfo((x, 0, z), (nowX, 0, nowZ), cost)
                        heapq.heappush(openNode, nodeInfo)
                        openDict[(x, 0, z)] = nodeInfo
            # if isOver:
            #     break
        if isOver:
            break

        # print '------------'
        # print len(openNode)
        # for m in openNode:
        #     print m.cost
        # print '------------'
        if len(openNode) == 0:
            return []
        nodeInfo = heapq.heappop(openNode)  #找到代价最小元素
        del openDict[nodeInfo.position]
        closeDict[nodeInfo.position] = nodeInfo
        nowX = nodeInfo.position[0]
        nowZ = nodeInfo.position[2]

    #将结果组织为链表，路径为从终点到起点
    res = [(endX, 0, endZ)]
    position = (nowX, 0, nowZ)
    while position:
        res.append(position)
        position = closeDict[position].parent
    return res


#常量定义
DATA_START_INDEX = 4
MAX_MES_LEN = 4096

LOGIN = 0
NO_NET = 0
LOGIN_INPUT_ERROR = 1
REGISTER = 1
REGISTER_RENAME = 1

MATCH_INFO = 5
ACK = 6

RIVAL_NUM = 4

RIVAL_REQUEST = 2   #敌人生成与控制的网络请求
RIVAL_CREATE = 0    #敌人生成
PLAYER_IN_SIGHT = 1 #在敌人视线内
TRACE = 2   #追踪玩家
PLAYER_IN_SHOOT = 3 #玩家在射程内
PLAYER_OUT_SHOOT = 4    #玩家从射程内到射程外
SHOOT = 5
#寻路
SEARCH_ROAD = 6

#控制生成敌人多少
minRivalNum = 1
maxRivalNum = 3

#读取地图信息
layoutFile = open('./layout.mesh', 'rb')
v3, layoutFile = Vector3.readNewFromFile(layoutFile)
layoutDict = {}
while v3:
    # print 'Tell', layoutFile.tell()
    data = layoutFile.read(4)

    # layoutDict[v3.x] = {}
    # layoutDict[v3.x][v3.y] = {}
    # layoutDict[v3.x][v3.y][v3.z] = struct.unpack_from('i', data, 0)[0]
    # print layoutDict[v3.x][v3.y][v3.z]
    layoutDict[(v3.x, 0, v3.z)] = struct.unpack_from('i', data, 0)[0]
    print layoutDict[(v3.x, 0, v3.z)]
    v3, layoutFile = Vector3.readNewFromFile(layoutFile)
    # print v3

serverSocket = socket(AF_INET, SOCK_DGRAM)
serverSocket.bind(("127.0.0.1", 999))
print "Server Start"
#数据库初始化操作
dbConnect = sqlite3.connect('PlayerInfo')
dbCursor = dbConnect.cursor()
dbCursor.execute('CREATE TABLE IF NOT EXISTS Players(userName TEXT PRIMARY KEY, password TEXT, killSum INTEGER, longestAliveTime REAL)')

while True:
    dataPack, addr = serverSocket.recvfrom(MAX_MES_LEN)
    # print 'Received from ', addr
    dataType = struct.unpack_from('i', dataPack, 0)[0]
    # print 'Data type', dataType

    if dataType == LOGIN:    #LogIn
        length = struct.unpack_from('ii', dataPack, DATA_START_INDEX)
        userName = struct.unpack_from(repr(length[0]) + 's', dataPack, DATA_START_INDEX + 8)[0]
        password = struct.unpack_from(repr(length[1]) + 's', dataPack, DATA_START_INDEX + 8 + length[0])[0]
        print "User name:", userName
        print "Password:", password
        dbCursor.execute("SELECT * FROM Players WHERE userName = '{}' AND password = '{}'".format(userName, password))
        result = dbCursor.fetchall()
        if(len(result) == 0):
            print 'Fail'
            serverSocket.sendto(struct.pack('=i?i', LOGIN, False, LOGIN_INPUT_ERROR), addr)
        else:   #登陆成功
            print 'Suc'
            print result[0][0],result[0][1],result[0][2],result[0][3], type(result[0][2]), sys.getsizeof(result[0][2])
            serverSocket.sendto(struct.pack('=i?id', LOGIN, True, result[0][2], result[0][3]), addr)
        # try:
        #     readf = open("./" + userName + '.userInfo', 'rb')
        # except IOError:
        #     serverSocket.sendto(struct.pack('i?i', LOG_IN, False, LOGIN_INPUT_ERROR), addr)
    elif dataType == REGISTER:
        length = struct.unpack_from('=ii', dataPack, DATA_START_INDEX)
        userName = struct.unpack_from(repr(length[0]) + 's', dataPack, DATA_START_INDEX + 8)[0]
        password = struct.unpack_from(repr(length[1]) + 's', dataPack, DATA_START_INDEX + 8 + length[0])[0]
        dbCursor.execute("SELECT * FROM Players WHERE userName = '{}'".format(userName))
        result = dbCursor.fetchall()
        if(len(result) != 0):
            serverSocket.sendto(struct.pack('=i?i', REGISTER, False, REGISTER_RENAME), addr)
        else:
            dbCursor.execute("INSERT INTO Players VALUES ('{}', '{}', {}, {})".format(userName, password, 0, 0.0))
            serverSocket.sendto(struct.pack('=i?', REGISTER, True), addr)

    elif dataType == RIVAL_NUM:
        rivalNum = struct.unpack_from('i', dataPack, DATA_START_INDEX)[0]
        createNum = random.randint(minRivalNum, maxRivalNum) - rivalNum
        if createNum < 0:
            createNum = 0
        serverSocket.sendto(struct.pack('=ii', RIVAL_NUM, createNum),addr)

    elif dataType == RIVAL_REQUEST:
        id = struct.unpack_from('i', dataPack, DATA_START_INDEX)[0]
        request = struct.unpack_from('i', dataPack, DATA_START_INDEX + 4)[0]
        if request == PLAYER_IN_SIGHT:
            serverSocket.sendto(struct.pack('=iii', RIVAL_REQUEST, id, TRACE), addr)
        elif request == PLAYER_IN_SHOOT:
            serverSocket.sendto(struct.pack('=iii', RIVAL_REQUEST, id, SHOOT), addr)
        elif request == PLAYER_OUT_SHOOT:
            serverSocket.sendto(struct.pack('=iii', RIVAL_REQUEST, id, TRACE), addr)
        elif request == SEARCH_ROAD:
            startX = struct.unpack_from('i', dataPack, DATA_START_INDEX + 8)[0]
            startZ = struct.unpack_from('i', dataPack, DATA_START_INDEX + 12)[0]
            endX = struct.unpack_from('i', dataPack, DATA_START_INDEX + 16)[0]
            endZ = struct.unpack_from('i', dataPack, DATA_START_INDEX + 20)[0]
            res = searchRoad(startX, startZ, endX, endZ, layoutDict)

            strList = []  # 减少字符串合并
            strList.append(struct.pack('=iii', RIVAL_REQUEST, id, SEARCH_ROAD))
            i = len(res) - 2  # 不传回起始坐标
            while i >= 0:
                strList.append(struct.pack('=ii', res[i][0], res[i][2]))
                i -= 1
            serverSocket.sendto(''.join(strList), addr)

    elif dataType == MATCH_INFO:
        kill = struct.unpack_from('i', dataPack, DATA_START_INDEX)[0]
        time = struct.unpack_from('d', dataPack, DATA_START_INDEX + 4)[0]
        nameLen = struct.unpack_from('i', dataPack, DATA_START_INDEX + 12)[0]
        name = struct.unpack_from(repr(nameLen) + 's', dataPack, DATA_START_INDEX + 16)[0]
        dbCursor.execute("SELECT * FROM Players WHERE userName = '{}'".format(name))
        result = dbCursor.fetchall()
        if len(result) != 0:
            print name, kill
            if time > result[0][3]:
                dbCursor.execute("UPDATE Players SET killSum = {}, longestAliveTime = {} WHERE userName = '{}'".format(kill, time, name))
            else:
                dbCursor.execute("UPDATE Players SET killSum = {} WHERE userName = '{}'".format(kill, name))
            serverSocket.sendto(struct.pack('=ii', MATCH_INFO, ACK), addr)



    #最后将操作更新至数据库
    dbConnect.commit()

serverSocket.close()


# r = searchRoad(37, 32, -16, 20, layoutDict)
# print '----------------------------------'
# for p in r:
#     print p
