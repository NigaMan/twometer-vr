//
// Created by twome on 06/04/2020.
//

#ifndef TVRDRV_DATAPACKET_H
#define TVRDRV_DATAPACKET_H

#include <vector>
#include "ControllerState.h"

struct DataPacket {
public:
    std::vector<ControllerState> controllerStates;

};

#endif //TVRDRV_DATAPACKET_H